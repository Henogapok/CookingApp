using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Queries;

public class GetSourceTypesQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetSourceTypesQuery, Result<List<ReferenceEntityDto>>>
{
    public Task<Result<List<ReferenceEntityDto>>> Handle(GetSourceTypesQuery request, CancellationToken cancellationToken) =>
        repository.GetSourceTypesAsync(cancellationToken);
}
