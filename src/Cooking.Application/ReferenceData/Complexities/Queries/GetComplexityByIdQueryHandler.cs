using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Queries;

public class GetComplexityByIdQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetComplexityByIdQuery, Result<ReferenceEntityDto>>
{
    public Task<Result<ReferenceEntityDto>> Handle(GetComplexityByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetComplexityByIdAsync(request.Id, cancellationToken);
}
