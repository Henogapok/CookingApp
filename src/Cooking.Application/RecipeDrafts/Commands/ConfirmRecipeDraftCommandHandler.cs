using Cooking.Application.Common.Errors;
using Cooking.Application.Ingredients;
using Cooking.Application.Recipes;
using Cooking.Domain.ReferenceData;
using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ConfirmRecipeDraftCommandHandler(
    IRecipeDraftRepositoryService drafts,
    IIngredientCatalogRepositoryService catalog,
    IRecipeRepositoryService recipes)
    : IRequestHandler<ConfirmRecipeDraftCommand, Result<Guid>>
{
    private static readonly RecipeFieldsValidator FieldsValidator = new();

    public async Task<Result<Guid>> Handle(ConfirmRecipeDraftCommand request, CancellationToken cancellationToken)
    {
        var content = await drafts.GetContentAsync(request.DraftId, request.UserId, cancellationToken);
        if (content.IsFailed)
            return content.ToResult<Guid>();

        var existing = await catalog.GetAllAsync(cancellationToken);
        if (existing.IsFailed)
            return existing.ToResult<Guid>();

        var catalogIdsByKey = new Dictionary<string, Guid>();
        foreach (var item in existing.Value)
            catalogIdsByKey.TryAdd(RecipeDraftMapper.NameKey(item.Name), item.Id);

        // Сверяем по названию ещё раз: пока черновик ждал, такой ингредиент мог появиться в каталоге.
        foreach (var ingredient in content.Value.Ingredients)
        {
            var key = RecipeDraftMapper.NameKey(ingredient.Name);
            if (ingredient.NewIngredient is not { } data || catalogIdsByKey.ContainsKey(key))
                continue;

            var created = await catalog.CreateAsync(
                new IngredientCatalogFields(
                    ingredient.Name,
                    data.CategoryId,
                    data.BaseUnitId,
                    PricePer100G: 0, // цену LLM оценивает плохо — вносится вручную
                    data.CaloriesPer100G,
                    data.ProteinPer100G,
                    data.FatPer100G,
                    data.CarbsPer100G,
                    CreatedBySourceId: ReferenceIds.DataSources.Llm,
                    NutritionSourceId: ReferenceIds.DataSources.Llm),
                cancellationToken);

            if (created.IsFailed)
                return created;

            catalogIdsByKey[key] = created.Value;
        }

        var fields = RecipeDraftMapper.ToRecipeFields(content.Value, catalogIdsByKey);

        var validation = FieldsValidator.Validate(fields);
        if (!validation.IsValid)
            return Result.Fail(new AppError(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), ErrorCode.Validation));

        var recipeId = await recipes.CreateAsync(request.UserId, fields, cancellationToken);
        if (recipeId.IsFailed)
            return recipeId;

        await drafts.DeleteAsync(request.DraftId, request.UserId, cancellationToken);

        return recipeId;
    }
}
