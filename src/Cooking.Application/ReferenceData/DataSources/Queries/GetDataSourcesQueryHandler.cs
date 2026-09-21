using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.DataSources.Queries;

public class GetDataSourcesQueryHandler(IReferenceDataRepositoryService repository)
    : IRequestHandler<GetDataSourcesQuery, Result<List<ReferenceEntityDto>>>
{
    public Task<Result<List<ReferenceEntityDto>>> Handle(GetDataSourcesQuery request, CancellationToken cancellationToken) =>
        repository.GetDataSourcesAsync(cancellationToken);
}
