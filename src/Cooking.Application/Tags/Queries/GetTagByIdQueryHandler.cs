using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Queries;

public class GetTagByIdQueryHandler(ITagRepositoryService repository)
    : IRequestHandler<GetTagByIdQuery, Result<TagDto>>
{
    public Task<Result<TagDto>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
