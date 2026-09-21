using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Queries;

public class GetSourceTypeByIdQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetSourceTypeByIdQuery, Result<ReferenceEntityDto>>
{
    public Task<Result<ReferenceEntityDto>> Handle(GetSourceTypeByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetSourceTypeByIdAsync(request.Id, cancellationToken);
}
