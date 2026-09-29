using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CancelRecipeDraftCommandHandler(IRecipeDraftRepositoryService drafts)
    : IRequestHandler<CancelRecipeDraftCommand, Result>
{
    public Task<Result> Handle(CancelRecipeDraftCommand request, CancellationToken cancellationToken) =>
        drafts.DeleteAsync(request.DraftId, request.UserId, cancellationToken);
}
