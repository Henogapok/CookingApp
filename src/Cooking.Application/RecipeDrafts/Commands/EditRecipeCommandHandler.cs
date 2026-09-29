using Cooking.Application.Recipes;
using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class EditRecipeCommandHandler(
    IRecipeRepositoryService recipes,
    IRecipeDraftRepositoryService drafts,
    IRecipeParsingQueue queue)
    : IRequestHandler<EditRecipeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(EditRecipeCommand request, CancellationToken cancellationToken)
    {
        // Та же проверка доступа, что и при чтении: чужой рецепт — NotFound.
        var recipe = await recipes.GetByIdAsync(request.RecipeId, request.UserId, cancellationToken);
        if (recipe.IsFailed)
            return recipe.ToResult<Guid>();

        var draftId = await drafts.CreateForEditAsync(
            request.UserId,
            // Исходного текста у сохранённого рецепта нет — всё нужное LLM возьмёт из текущей версии.
            $"Сохранённый рецепт «{recipe.Value.Title}»",
            RecipeDraftMapper.FromRecipe(recipe.Value),
            request.Text.Trim(),
            cancellationToken);

        if (draftId.IsFailed)
            return draftId;

        await queue.EnqueueAsync(new RecipeParsingJob(draftId.Value), cancellationToken);

        return draftId;
    }
}
