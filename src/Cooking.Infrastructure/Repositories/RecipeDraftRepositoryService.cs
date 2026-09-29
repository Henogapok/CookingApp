using System.Text.Json;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.RecipeDrafts;
using Cooking.Application.Recipes;
using Cooking.Domain.Entities.Recipes;
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

    public async Task<Result<RecipeDraftSource>> GetSourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dataContext.RecipeDrafts
            .Where(d => d.Id == id)
            .Select(d => new RecipeDraftSource(d.Id, d.UserId, d.SourceText))
            .FirstOrDefaultAsync(cancellationToken);

        return source is null ? DraftNotFound(id) : Result.Ok(source);
    }

    public async Task<Result> SetContentAsync(Guid id, RecipeDraftContent content, CancellationToken cancellationToken)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);
        if (draft is null)
            return DraftNotFound(id);

        draft.ContentJson = JsonSerializer.Serialize(content, JsonOptions);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
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
        var draft = await FindOwnAsync(id, userId, cancellationToken);
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

        var unitIds = c.Ingredients.Where(i => i.UnitId is not null).Select(i => i.UnitId!.Value).Distinct().ToList();
        var unitAbbreviations = await dataContext.MeasurementUnits
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Abbreviation, cancellationToken);

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
            c.Ingredients
                .Select(i => new RecipeDraftIngredientDto(
                    i.Name,
                    i.Amount,
                    i.UnitId is { } unitId ? unitAbbreviations.GetValueOrDefault(unitId) : null,
                    IsNew: i.NewIngredient is not null))
                .ToList(),
            c.Steps.Select((s, index) => new RecipeStepDto(index + 1, s.Instruction, s.TimerSeconds)).ToList(),
            tagNames,
            draft.Value.ExpiresAt));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var draft = await FindOwnAsync(id, userId, cancellationToken, requireParsed: false);
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

    /// <summary>Чужой и просроченный черновик для пользователя «не существует».</summary>
    private async Task<Result<RecipeDraft>> FindOwnAsync(
        Guid id, Guid userId, CancellationToken cancellationToken, bool requireParsed = true)
    {
        var draft = await dataContext.RecipeDrafts.FindAsync([id], cancellationToken);

        if (draft is null || draft.UserId != userId || draft.ExpiresAt < DateTime.UtcNow)
            return DraftNotFound(id);

        if (requireParsed && draft.ContentJson is null)
            return Result.Fail(new AppError($"Recipe draft '{id}' is still being parsed.", ErrorCode.LogicConflict));

        return Result.Ok(draft);
    }

    private static Result<RecipeDraftContent> Deserialize(RecipeDraft draft) =>
        JsonSerializer.Deserialize<RecipeDraftContent>(draft.ContentJson!, JsonOptions) is { } content
            ? Result.Ok(content)
            : Result.Fail(new AppError($"Recipe draft '{draft.Id}' has invalid content.", ErrorCode.LogicConflict));

    private static Result DraftNotFound(Guid id) =>
        Result.Fail(new AppError($"Recipe draft with id '{id}' was not found.", ErrorCode.NotFound));
}
