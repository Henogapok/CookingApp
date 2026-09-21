using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Queries;

public class GetTagTypeByIdQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetTagTypeByIdQuery, Result<ReferenceEntityDto>>
{
    public Task<Result<ReferenceEntityDto>> Handle(GetTagTypeByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetTagTypeByIdAsync(request.Id, cancellationToken);
}
