using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CorrectRecipeDraftCommandHandler(IRecipeDraftRepositoryService drafts, IRecipeParsingQueue queue)
    : IRequestHandler<CorrectRecipeDraftCommand, Result>
{
    public async Task<Result> Handle(CorrectRecipeDraftCommand request, CancellationToken cancellationToken)
    {
        var started = await drafts.StartCorrectionAsync(request.DraftId, request.UserId, request.Text.Trim(), cancellationToken);
        if (started.IsFailed)
            return started;

        // Та же задача, что и для первого разбора: текст правки уже лежит в черновике.
        await queue.EnqueueAsync(new RecipeParsingJob(request.DraftId), cancellationToken);

        return Result.Ok();
    }
}
