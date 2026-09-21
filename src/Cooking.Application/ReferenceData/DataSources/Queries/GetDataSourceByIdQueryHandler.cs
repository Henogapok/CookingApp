using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Queries;

public class GetDataSourceByIdQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetDataSourceByIdQuery, Result<ReferenceEntityDto>>
{
    public Task<Result<ReferenceEntityDto>> Handle(GetDataSourceByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetDataSourceByIdAsync(request.Id, cancellationToken);
}
