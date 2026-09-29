using System.Linq.Expressions;
using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Nutrition;
using Cooking.Application.Recipes;
using Cooking.Application.Tags;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;
using Cooking.Domain.ReferenceData;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Infrastructure.Repositories;

public class RecipeRepositoryService(IDataContext dataContext) : IRecipeRepositoryService
{
    public async Task<Result<Guid>> CreateAsync(Guid userId, RecipeFields fields, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var referencesCheck = await ValidateReferencesAsync(fields, cancellationToken);
        if (referencesCheck.IsFailed)
            return referencesCheck.ToResult<Guid>();

        var recipe = new Recipe { Title = fields.Title, CreatedByUserId = userId };
        ApplyFields(recipe, fields);

        dataContext.Recipes.Add(recipe);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(recipe.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, Guid userId, RecipeFields fields, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        // Редактировать может любой, кто видит рецепт: автор и его семья.
        var recipe = await dataContext.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.RecipeTags)
            .Where(VisibleTo(user))
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recipe is null)
            return RecipeNotFound(id);

        var referencesCheck = await ValidateReferencesAsync(fields, cancellationToken);
        if (referencesCheck.IsFailed)
            return referencesCheck;

        ApplyFields(recipe, fields);

        // Изменение одних только ингредиентов/шагов/тегов не помечает сам рецепт изменённым —
        // трогаем UpdatedAt явно, чтобы он отражал любую правку (точное значение проставит SaveChanges).
        recipe.UpdatedAt = DateTime.UtcNow;

        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var recipe = await dataContext.Recipes
            .Where(VisibleTo(user))
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recipe is null)
            return RecipeNotFound(id);

        if (recipe.CreatedByUserId != userId)
            return Result.Fail(new AppError("Only the author can delete a recipe.", ErrorCode.Forbidden));

        recipe.DeletedAt = DateTime.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<RecipeDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var row = await dataContext.Recipes
            .Where(VisibleTo(user))
            .Where(r => r.Id == id)
            .Select(r => new RecipeRow(
                r.Id,
                r.Title,
                r.Description,
                r.SourceUrl,
                r.SourceTypeId,
                r.SourceType.Name,
                r.ComplexityId,
                r.Complexity.Name,
                r.Servings,
                r.CookingTimeMinutes,
                r.CreatedByUserId,
                r.CreatedByUser.FirstName,
                r.CreatedAt,
                r.UpdatedAt,
                r.Ingredients
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new IngredientRow(
                        i.IngredientCatalogId,
                        i.IngredientCatalog.Name,
                        i.Amount,
                        i.UnitId,
                        i.Unit != null ? i.Unit.Name : null,
                        i.Unit != null ? i.Unit.Abbreviation : null,
                        i.IngredientCatalog.NutritionSourceId == ReferenceIds.DataSources.Llm,
                        i.IngredientCatalog.BaseUnitId,
                        i.IngredientCatalog.BaseUnit.Abbreviation,
                        i.IngredientCatalog.PieceWeight,
                        i.IngredientCatalog.CaloriesPer100g,
                        i.IngredientCatalog.ProteinPer100g,
                        i.IngredientCatalog.FatPer100g,
                        i.IngredientCatalog.CarbsPer100g,
                        i.IngredientCatalog.PricePer100g))
                    .ToList(),
                r.Steps
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new RecipeStepDto(s.StepNumber, s.Instruction, s.TimerSeconds))
                    .ToList(),
                r.RecipeTags
                    .OrderBy(rt => rt.Tag.Name)
                    .Select(rt => new TagDto(rt.TagId, rt.Tag.Name, rt.Tag.TagTypeId, rt.Tag.TagType.Name))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? RecipeNotFound(id) : Result.Ok(ToDto(row));
    }

    // КБЖУ считается в C# после загрузки (NutritionCalculator), поэтому сначала — плоские строки из БД.
    private sealed record IngredientRow(
        Guid IngredientCatalogId,
        string Name,
        decimal? Amount,
        Guid? UnitId,
        string? UnitName,
        string? UnitAbbreviation,
        bool IsNutritionEstimatedByLlm,
        Guid BaseUnitId,
        string BaseUnitAbbreviation,
        decimal? PieceWeight,
        decimal Calories,
        decimal Protein,
        decimal Fat,
        decimal Carbs,
        decimal Price)
    {
        public IngredientAmount ToAmount() => new(
            Name, Amount, UnitId,
            new IngredientNutritionSource(BaseUnitId, PieceWeight, new NutritionFacts(Calories, Protein, Fat, Carbs), Price));
    }

    private sealed record RecipeRow(
        Guid Id,
        string Title,
        string? Description,
        string? SourceUrl,
        Guid SourceTypeId,
        string SourceTypeName,
        Guid ComplexityId,
        string ComplexityName,
        int? Servings,
        int? CookingTimeMinutes,
        Guid CreatedByUserId,
        string CreatedByFirstName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<IngredientRow> Ingredients,
        List<RecipeStepDto> Steps,
        List<TagDto> Tags);

    private static RecipeDto ToDto(RecipeRow r)
    {
        var ingredients = r.Ingredients
            .Select(i =>
            {
                var nutrition = NutritionCalculator.ForIngredient(i.ToAmount());
                return new RecipeIngredientDto(
                    i.IngredientCatalogId, i.Name, i.Amount, i.UnitId, i.UnitName, i.UnitAbbreviation,
                    i.IsNutritionEstimatedByLlm, nutrition.BaseAmount, i.BaseUnitAbbreviation, nutrition.Nutrition);
            })
            .ToList();

        return new RecipeDto(
            r.Id, r.Title, r.Description, r.SourceUrl,
            r.SourceTypeId, r.SourceTypeName, r.ComplexityId, r.ComplexityName,
            r.Servings, r.CookingTimeMinutes,
            r.CreatedByUserId, r.CreatedByFirstName, r.CreatedAt, r.UpdatedAt,
            ingredients, r.Steps, r.Tags,
            NutritionCalculator.ForRecipe(r.Ingredients.Select(i => i.ToAmount()).ToList(), r.Servings));
    }

    public async Task<Result<List<RecipeSummaryDto>>> GetAllAsync(Guid userId, string? search, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var query = dataContext.Recipes.Where(VisibleTo(user));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower + Contains: Npgsql переводит в lower(...) LIKE, и это же работает в InMemory-тестах.
            // Полнотекстовый поиск — отдельной задачей.
            var term = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(term));
        }

        var recipes = await query
            .OrderBy(r => r.Title)
            .Select(r => new RecipeSummaryDto(
                r.Id,
                r.Title,
                r.Complexity.Name,
                r.Servings,
                r.CookingTimeMinutes,
                r.CreatedByUserId,
                r.CreatedByUser.FirstName))
            .ToListAsync(cancellationToken);

        return Result.Ok(recipes);
    }

    /// <summary>Рецепт виден автору и участникам его текущей семьи.</summary>
    private static Expression<Func<Recipe, bool>> VisibleTo(User user)
    {
        var userId = user.Id;
        var familyId = user.FamilyId;

        return r => r.CreatedByUserId == userId
                    || (familyId != null && r.CreatedByUser.FamilyId == familyId);
    }

    /// <summary>Перезаписывает поля и дочерние коллекции рецепта значениями из fields.</summary>
    private static void ApplyFields(Recipe recipe, RecipeFields fields)
    {
        recipe.Title = fields.Title;
        recipe.Description = fields.Description;
        recipe.SourceUrl = fields.SourceUrl;
        recipe.SourceTypeId = fields.SourceTypeId;
        recipe.ComplexityId = fields.ComplexityId;
        recipe.Servings = fields.Servings;
        recipe.CookingTimeMinutes = fields.CookingTimeMinutes;

        // Ингредиенты и шаги заменяются целиком: удалённые из коллекции строки EF удалит как сироты.
        recipe.Ingredients.Clear();
        foreach (var (ingredient, index) in fields.Ingredients.Select((x, i) => (x, i)))
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                IngredientCatalogId = ingredient.IngredientCatalogId,
                Amount = ingredient.Amount,
                UnitId = ingredient.UnitId,
                SortOrder = index + 1,
            });
        }

        recipe.Steps.Clear();
        foreach (var (step, index) in fields.Steps.Select((x, i) => (x, i)))
        {
            recipe.Steps.Add(new RecipeStep
            {
                StepNumber = index + 1,
                Instruction = step.Instruction,
                TimerSeconds = step.TimerSeconds,
            });
        }

        // У RecipeTag составной ключ (RecipeId, TagId): удалить и тут же добавить ту же пару
        // в одном SaveChanges нельзя, поэтому теги синхронизируем разницей.
        var newTagIds = fields.TagIds.ToHashSet();

        foreach (var removed in recipe.RecipeTags.Where(rt => !newTagIds.Contains(rt.TagId)).ToList())
            recipe.RecipeTags.Remove(removed);

        var existingTagIds = recipe.RecipeTags.Select(rt => rt.TagId).ToHashSet();

        foreach (var tagId in newTagIds.Where(id => !existingTagIds.Contains(id)))
            recipe.RecipeTags.Add(new RecipeTag { TagId = tagId });
    }

    private async Task<Result> ValidateReferencesAsync(RecipeFields fields, CancellationToken cancellationToken)
    {
        if (!await dataContext.SourceTypes.AnyAsync(x => x.Id == fields.SourceTypeId, cancellationToken))
            return Result.Fail(new AppError($"SourceType with id '{fields.SourceTypeId}' was not found.", ErrorCode.Validation));

        if (!await dataContext.Complexities.AnyAsync(x => x.Id == fields.ComplexityId, cancellationToken))
            return Result.Fail(new AppError($"Complexity with id '{fields.ComplexityId}' was not found.", ErrorCode.Validation));

        var missingIngredients = await FindMissingAsync(
            dataContext.IngredientCatalog.Select(x => x.Id),
            fields.Ingredients.Select(x => x.IngredientCatalogId), cancellationToken);
        if (missingIngredients.Count > 0)
            return MissingReferences("IngredientCatalog", missingIngredients);

        var missingUnits = await FindMissingAsync(
            dataContext.MeasurementUnits.Select(x => x.Id),
            fields.Ingredients.Where(x => x.UnitId is not null).Select(x => x.UnitId!.Value), cancellationToken);
        if (missingUnits.Count > 0)
            return MissingReferences("MeasurementUnit", missingUnits);

        var missingTags = await FindMissingAsync(
            dataContext.Tags.Select(x => x.Id),
            fields.TagIds, cancellationToken);
        if (missingTags.Count > 0)
            return MissingReferences(nameof(Tag), missingTags);

        return Result.Ok();
    }

    private static async Task<List<Guid>> FindMissingAsync(
        IQueryable<Guid> existingIds, IEnumerable<Guid> requestedIds, CancellationToken cancellationToken)
    {
        var requested = requestedIds.Distinct().ToList();

        if (requested.Count == 0)
            return [];

        var found = await existingIds.Where(id => requested.Contains(id)).ToListAsync(cancellationToken);

        return requested.Except(found).ToList();
    }

    private static Result MissingReferences(string entityName, List<Guid> ids) =>
        Result.Fail(new AppError(
            $"{entityName} with id(s) {string.Join(", ", ids.Select(id => $"'{id}'"))} was not found.",
            ErrorCode.Validation));

    private static Result UserNotFound(Guid userId) =>
        Result.Fail(new AppError($"User with id '{userId}' was not found.", ErrorCode.Validation));

    private static Result RecipeNotFound(Guid id) =>
        Result.Fail(new AppError($"Recipe with id '{id}' was not found.", ErrorCode.NotFound));
}
