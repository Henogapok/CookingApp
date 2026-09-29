using System.Text.Json;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Nutrition;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.Recipes;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.ReferenceData;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class RecipeDraftRepositoryService(IDataContext dataContext) : IRecipeDraftRepositoryService
{
    /// <summary>Сколько черновик ждёт подтверждения.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<Guid>> CreateAsync(Guid userId, string sourceText, CancellationToken cancellationToken)
    {
        if (!await dataContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            return Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

        var now = DateTime.UtcNow;

        // Отдельной фоновой чистки нет: просроченные черновики убираем, когда появляется новый.
        var expired = await dataContext.RecipeDrafts.Where(d => d.ExpiresAt < now).ToListAsync(cancellationToken);
        dataContext.RecipeDrafts.RemoveRange(expired);

        var draft = new RecipeDraft { UserId = userId, SourceText = sourceText, ExpiresAt = now + Lifetime };
        dataContext.RecipeDrafts.Add(draft);

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(draft.Id);
    }

    public async Task<Result<Guid>> CreateForEditAsync(
        Guid userId, string sourceText, RecipeDraftContent content, string correction, CancellationToken cancellationToken)
    {
        var created = await CreateAsync(userId, sourceText, cancellationToken);
        if (created.IsFailed)
            return created;

        var draft = await dataContext.RecipeDrafts.FindAsync([created.Value], cancellationToken);
        draft!.ContentJson = JsonSerializer.Serialize(content, JsonOptions);
        draft.PendingCorrection = correction;
        await dataContext.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<Result<RecipeDraftSource>> GetSourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);
        if (draft is null)
            return DraftNotFound(id);

        var content = draft.ContentJson is null ? null : Deserialize(draft);
        if (content is { IsFailed: true })
            return content.ToResult<RecipeDraftSource>();

        return Result.Ok(new RecipeDraftSource(draft.Id, draft.UserId, draft.SourceText, content?.Value, draft.PendingCorrection));
    }

    public async Task<Result> SetContentAsync(Guid id, RecipeDraftContent content, CancellationToken cancellationToken)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);
        if (draft is null)
            return DraftNotFound(id);

        draft.ContentJson = JsonSerializer.Serialize(content, JsonOptions);
        draft.PendingCorrection = null;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> StartCorrectionAsync(Guid id, Guid userId, string correction, CancellationToken cancellationToken)
    {
        var draft = await FindOwnAsync(id, userId, cancellationToken);
        if (draft.IsFailed)
            return draft.ToResult();

        draft.Value.PendingCorrection = correction;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task ClearCorrectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);
        if (draft is null)
            return;

        draft.PendingCorrection = null;
        await dataContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<RecipeDraftContent>> GetContentAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var draft = await FindOwnAsync(id, userId, cancellationToken);
        if (draft.IsFailed)
            return draft.ToResult<RecipeDraftContent>();

        return Deserialize(draft.Value);
    }

    public async Task<Result<RecipeDraftDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        // Показать можно и во время правки — это последняя готовая версия.
        var draft = await FindOwnAsync(id, userId, cancellationToken, allowCorrecting: true);
        if (draft.IsFailed)
            return draft.ToResult<RecipeDraftDto>();

        var content = Deserialize(draft.Value);
        if (content.IsFailed)
            return content.ToResult<RecipeDraftDto>();

        var c = content.Value;

        // Черновик хранит только Id — названия для показа подтягиваем из справочников.
        var complexityName = await dataContext.Complexities
            .Where(x => x.Id == c.ComplexityId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        // Для КБЖУ: ингредиенты из каталога берут данные оттуда, новые — из оценки ИИ в черновике.
        var catalogIds = c.Ingredients.Where(i => i.IngredientCatalogId is not null).Select(i => i.IngredientCatalogId!.Value).ToList();
        var catalog = await dataContext.IngredientCatalog
            .Where(x => catalogIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var unitIds = c.Ingredients.Where(i => i.UnitId is not null).Select(i => i.UnitId!.Value)
            .Concat(c.Ingredients.Select(i => i.NewIngredient?.BaseUnitId).OfType<Guid>())
            .Concat(catalog.Values.Select(x => x.BaseUnitId))
            .Distinct()
            .ToList();
        var unitAbbreviations = await dataContext.MeasurementUnits
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Abbreviation, cancellationToken);

        IngredientNutritionSource? SourceOf(RecipeDraftIngredient i)
        {
            if (i.NewIngredient is { } n)
                return new IngredientNutritionSource(
                    n.BaseUnitId, i.PieceWeight,
                    new NutritionFacts(n.CaloriesPer100G, n.ProteinPer100G, n.FatPer100G, n.CarbsPer100G), 0);

            if (i.IngredientCatalogId is { } catalogId && catalog.TryGetValue(catalogId, out var x))
                return new IngredientNutritionSource(
                    x.BaseUnitId, x.PieceWeight ?? i.PieceWeight, // вес штуки из черновика допишется в каталог при сохранении
                    new NutritionFacts(x.CaloriesPer100g, x.ProteinPer100g, x.FatPer100g, x.CarbsPer100g), x.PricePer100g);

            return null; // ингредиент успели удалить из каталога
        }

        var amounts = c.Ingredients
            .Select(i => SourceOf(i) is { } source ? new IngredientAmount(i.Name, i.Amount, i.UnitId, source) : null)
            .ToList();

        var tagNames = await dataContext.Tags
            .Where(t => c.TagIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);

        return Result.Ok(new RecipeDraftDto(
            draft.Value.Id,
            c.Title,
            c.Description,
            complexityName,
            c.EffectiveServings,
            ServingsIsEstimate: c.Servings is null && c.EffectiveServings is not null,
            c.EffectiveCookingTimeMinutes,
            CookingTimeIsEstimate: c.CookingTimeMinutes is null && c.EffectiveCookingTimeMinutes is not null,
            c.CanApplyEstimates,
            IsBeingCorrected: draft.Value.PendingCorrection is not null,
            c.RecipeId,
            c.Ingredients
                .Select((i, index) =>
                {
                    var nutrition = amounts[index] is { } amount ? NutritionCalculator.ForIngredient(amount) : null;
                    return new RecipeDraftIngredientDto(
                        i.Name,
                        i.Amount,
                        i.UnitId,
                        i.UnitId is { } unitId ? unitAbbreviations.GetValueOrDefault(unitId) : null,
                        IsNew: i.NewIngredient is not null,
                        IsNutritionEstimatedByLlm: i.NewIngredient is not null
                            || (i.IngredientCatalogId is { } id && catalog.TryGetValue(id, out var x) && x.NutritionSourceId == ReferenceIds.DataSources.Llm),
                        nutrition?.BaseAmount,
                        amounts[index] is { } a ? unitAbbreviations.GetValueOrDefault(a.Source.BaseUnitId) : null,
                        nutrition?.Nutrition);
                })
                .ToList(),
            c.Steps.Select((s, index) => new RecipeStepDto(index + 1, s.Instruction, s.TimerSeconds)).ToList(),
            tagNames,
            NutritionCalculator.ForRecipe(amounts.OfType<IngredientAmount>().ToList(), c.EffectiveServings),
            draft.Value.ExpiresAt));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var draft = await FindOwnAsync(id, userId, cancellationToken, requireParsed: false, allowCorrecting: true);
        if (draft.IsFailed)
            return draft.ToResult();

        dataContext.RecipeDrafts.Remove(draft.Value);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task DiscardAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);
        if (draft is null)
            return;

        dataContext.RecipeDrafts.Remove(draft);
        await dataContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Чужой и просроченный черновик для пользователя «не существует». По умолчанию черновик ещё и должен быть готов:
    /// разобран и без незавершённой правки — иначе сохранить или поправить можно было бы устаревшую версию.
    /// </summary>
    private async Task<Result<RecipeDraft>> FindOwnAsync(
        Guid id, Guid userId, CancellationToken cancellationToken, bool requireParsed = true, bool allowCorrecting = false)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);

        if (draft is null || draft.UserId != userId || draft.ExpiresAt < DateTime.UtcNow)
            return DraftNotFound(id);

        if (requireParsed && draft.ContentJson is null)
            return Result.Fail(new AppError($"Recipe draft '{id}' is still being parsed.", ErrorCode.LogicConflict));

        if (!allowCorrecting && draft.PendingCorrection is not null)
            return Result.Fail(new AppError($"Recipe draft '{id}' is being corrected.", ErrorCode.LogicConflict));

        return Result.Ok(draft);
    }

    private static Result<RecipeDraftContent> Deserialize(RecipeDraft draft) =>
        JsonSerializer.Deserialize<RecipeDraftContent>(draft.ContentJson!, JsonOptions) is { } content
            ? Result.Ok(content)
            : Result.Fail(new AppError($"Recipe draft '{draft.Id}' has invalid content.", ErrorCode.LogicConflict));

    private static Result DraftNotFound(Guid id) =>
        Result.Fail(new AppError($"Recipe draft with id '{id}' was not found.", ErrorCode.NotFound));
}
