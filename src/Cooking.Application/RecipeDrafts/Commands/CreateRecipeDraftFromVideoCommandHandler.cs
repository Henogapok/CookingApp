using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftFromVideoCommandHandler(IRecipeDraftRepositoryService drafts, IRecipeParsingQueue queue)
    : IRequestHandler<CreateRecipeDraftFromVideoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecipeDraftFromVideoCommand request, CancellationToken cancellationToken)
    {
        var result = await drafts.CreateFromMediaAsync(
            request.UserId, sourceUrl: null, request.VideoFilePath, request.Caption?.Trim(), cancellationToken);
        if (result.IsFailed)
            return result;

        await queue.EnqueueAsync(new RecipeParsingJob(result.Value), cancellationToken);
        return result;
    }
}
