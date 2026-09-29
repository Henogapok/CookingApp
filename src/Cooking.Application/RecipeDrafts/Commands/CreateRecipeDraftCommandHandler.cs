using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftCommandHandler(IRecipeDraftRepositoryService drafts, IRecipeParsingQueue queue)
    : IRequestHandler<CreateRecipeDraftCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRecipeDraftCommand request, CancellationToken cancellationToken)
    {
        var result = await drafts.CreateAsync(request.UserId, request.Text.Trim(), cancellationToken);
        if (result.IsFailed)
            return result;

        await queue.EnqueueAsync(new RecipeParsingJob(result.Value), cancellationToken);

        return result;
    }
}
