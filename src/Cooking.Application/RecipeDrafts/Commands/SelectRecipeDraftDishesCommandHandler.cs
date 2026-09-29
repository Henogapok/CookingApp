using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class SelectRecipeDraftDishesCommandHandler(IRecipeDraftRepositoryService drafts, IRecipeParsingQueue queue)
    : IRequestHandler<SelectRecipeDraftDishesCommand, Result>
{
    public async Task<Result> Handle(SelectRecipeDraftDishesCommand request, CancellationToken cancellationToken)
    {
        var selected = await drafts.SelectDishesAsync(request.DraftId, request.UserId, request.DishIndexes, cancellationToken);
        if (selected.IsFailed)
            return selected;

        // Та же задача разбора: выбор уже лежит в черновике.
        await queue.EnqueueAsync(new RecipeParsingJob(request.DraftId), cancellationToken);

        return Result.Ok();
    }
}
