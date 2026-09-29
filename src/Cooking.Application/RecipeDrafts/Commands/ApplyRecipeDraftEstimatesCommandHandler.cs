using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ApplyRecipeDraftEstimatesCommandHandler(IRecipeDraftRepositoryService drafts)
    : IRequestHandler<ApplyRecipeDraftEstimatesCommand, Result>
{
    public async Task<Result> Handle(ApplyRecipeDraftEstimatesCommand request, CancellationToken cancellationToken)
    {
        var content = await drafts.GetContentAsync(request.DraftId, request.UserId, cancellationToken);
        if (content.IsFailed)
            return content.ToResult();

        if (content.Value.UseEstimates)
            return Result.Ok();

        return await drafts.SetContentAsync(request.DraftId, content.Value with { UseEstimates = true }, cancellationToken);
    }
}
