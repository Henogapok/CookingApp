using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Queries;

public class GetTagsQueryHandler(ITagRepositoryService repository)
    : IRequestHandler<GetTagsQuery, Result<List<TagDto>>>
{
    public Task<Result<List<TagDto>>> Handle(GetTagsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(request.TagTypeId, cancellationToken);
}
