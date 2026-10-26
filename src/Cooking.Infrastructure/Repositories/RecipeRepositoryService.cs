using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using Cooking.Application.Nutrition;
using Cooking.Application.Recipes;
using Cooking.Application.Tags;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
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
            .Where(HouseholdAccess.Recipes(user))
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
            .Where(HouseholdAccess.Recipes(user))
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
            .Where(HouseholdAccess.Recipes(user))
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
                        i.IngredientCatalog.CarbsPer100g))
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

        if (row is null)
            return RecipeNotFound(id);

        var ingredientIds = row.Ingredients.Select(i => i.IngredientCatalogId).ToList();
        var products = await SelectedProducts.LoadAsync(dataContext, user, ingredientIds, cancellationToken);
        return Result.Ok(ToDto(row, products));
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
        decimal Carbs)
    {
        public IngredientAmount ToAmount(SelectedProduct? product) =>
            ToIngredientAmount(Name, Amount, UnitId, BaseUnitId, PieceWeight, new NutritionFacts(Calories, Protein, Fat, Carbs), product);
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

    private static RecipeDto ToDto(RecipeRow r, Dictionary<Guid, SelectedProduct> products)
    {
        var amounts = r.Ingredients.Select(i => i.ToAmount(products.GetValueOrDefault(i.IngredientCatalogId))).ToList();

        var ingredients = r.Ingredients
            .Select((i, index) =>
            {
                var product = products.GetValueOrDefault(i.IngredientCatalogId);
                var nutrition = NutritionCalculator.ForIngredient(amounts[index]);
                return new RecipeIngredientDto(
                    i.IngredientCatalogId, i.Name, i.Amount, i.UnitId, i.UnitName, i.UnitAbbreviation,
                    NutritionCalculator.IsEstimatedByLlm(i.IsNutritionEstimatedByLlm, product?.Values),
                    nutrition.BaseAmount, i.BaseUnitAbbreviation, nutrition.Nutrition,
                    product?.Id, product?.Name);
            })
            .ToList();

        return new RecipeDto(
            r.Id, r.Title, r.Description, r.SourceUrl,
            r.SourceTypeId, r.SourceTypeName, r.ComplexityId, r.ComplexityName,
            r.Servings, r.CookingTimeMinutes,
            r.CreatedByUserId, r.CreatedByFirstName, r.CreatedAt, r.UpdatedAt,
            ingredients, r.Steps, r.Tags,
            NutritionCalculator.ForRecipe(amounts, r.Servings));
    }

    public async Task<Result<List<RecipeSummaryDto>>> GetAllAsync(Guid userId, string? search, CancellationToken cancellationToken)
    {
        var user = await dataContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            return UserNotFound(userId);

        var query = dataContext.Recipes.Where(HouseholdAccess.Recipes(user));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower + Contains: Npgsql переводит в lower(...) LIKE, и это же работает в InMemory-тестах.
            // Полнотекстовый поиск — отдельной задачей.
            var term = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(term));
        }

        var rows = await query
            .OrderBy(r => r.Title)
            .Select(r => new RecipeSummaryRow(
                r.Id,
                r.Title,
                r.Complexity.Name,
                r.Servings,
                r.CookingTimeMinutes,
                r.CreatedByUserId,
                r.CreatedByUser.FirstName,
                r.CreatedAt,
                r.Ingredients
                    .Select(i => new NutritionRow(
                        i.IngredientCatalogId,
                        i.IngredientCatalog.Name,
                        i.Amount,
                        i.UnitId,
                        i.IngredientCatalog.BaseUnitId,
                        i.IngredientCatalog.PieceWeight,
                        i.IngredientCatalog.CaloriesPer100g,
                        i.IngredientCatalog.ProteinPer100g,
                        i.IngredientCatalog.FatPer100g,
                        i.IngredientCatalog.CarbsPer100g))
                    .ToList(),
                r.RecipeTags
                    .OrderBy(rt => rt.Tag.Name)
                    .Select(rt => new TagDto(rt.TagId, rt.Tag.Name, rt.Tag.TagTypeId, rt.Tag.TagType.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var products = await SelectedProducts.LoadAsync(dataContext, user, ingredientIds: null, cancellationToken);

        var recipes = rows
            .Select(r => new RecipeSummaryDto(
                r.Id, r.Title, r.ComplexityName, r.Servings, r.CookingTimeMinutes,
                r.CreatedByUserId, r.CreatedByFirstName, r.CreatedAt, r.Tags,
                NutritionCalculator.ForRecipe(
                    r.Ingredients.Select(i => i.ToAmount(products.GetValueOrDefault(i.IngredientCatalogId))).ToList(),
                    r.Servings).PerServing))
            .ToList();

        return Result.Ok(recipes);
    }

    // Для списка из каталога нужно только то, что идёт в расчёт КБЖУ, — без названий единиц и признаков LLM.
    private sealed record NutritionRow(
        Guid IngredientCatalogId,
        string Name,
        decimal? Amount,
        Guid? UnitId,
        Guid BaseUnitId,
        decimal? PieceWeight,
        decimal Calories,
        decimal Protein,
        decimal Fat,
        decimal Carbs)
    {
        public IngredientAmount ToAmount(SelectedProduct? product) =>
            ToIngredientAmount(Name, Amount, UnitId, BaseUnitId, PieceWeight, new NutritionFacts(Calories, Protein, Fat, Carbs), product);
    }

    /// <summary>Строка рецепта для расчёта: КБЖУ каталога, а с выбранным продуктом — его цена и КБЖУ с упаковки.</summary>
    private static IngredientAmount ToIngredientAmount(
        string name, decimal? amount, Guid? unitId, Guid baseUnitId, decimal? pieceWeight, NutritionFacts catalogPer100, SelectedProduct? product) =>
        new(name, amount, unitId, NutritionCalculator.WithProduct(
            new IngredientNutritionSource(baseUnitId, pieceWeight, catalogPer100, PricePer100: null), product?.Values));

    private sealed record RecipeSummaryRow(
        Guid Id,
        string Title,
        string ComplexityName,
        int? Servings,
        int? CookingTimeMinutes,
        Guid CreatedByUserId,
        string CreatedByFirstName,
        DateTime CreatedAt,
        List<NutritionRow> Ingredients,
        List<TagDto> Tags);

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
