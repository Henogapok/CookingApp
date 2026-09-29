using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Queries;

public class GetRecipeDraftQueryHandler(IRecipeDraftRepositoryService drafts)
    : IRequestHandler<GetRecipeDraftQuery, Result<RecipeDraftDto>>
{
    public Task<Result<RecipeDraftDto>> Handle(GetRecipeDraftQuery request, CancellationToken cancellationToken) =>
        drafts.GetByIdAsync(request.DraftId, request.UserId, cancellationToken);
}
