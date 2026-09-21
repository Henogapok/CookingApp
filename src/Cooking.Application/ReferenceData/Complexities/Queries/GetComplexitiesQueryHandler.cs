using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Queries;

public class GetComplexitiesQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetComplexitiesQuery, Result<List<ReferenceEntityDto>>>
{
    public Task<Result<List<ReferenceEntityDto>>> Handle(GetComplexitiesQuery request, CancellationToken cancellationToken) =>
        repository.GetComplexitiesAsync(cancellationToken);
}
