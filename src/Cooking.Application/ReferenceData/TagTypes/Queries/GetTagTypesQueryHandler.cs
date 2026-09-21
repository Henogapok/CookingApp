using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Queries;

public class GetTagTypesQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetTagTypesQuery, Result<List<ReferenceEntityDto>>>
{
    public Task<Result<List<ReferenceEntityDto>>> Handle(GetTagTypesQuery request, CancellationToken cancellationToken) =>
        repository.GetTagTypesAsync(cancellationToken);
}
