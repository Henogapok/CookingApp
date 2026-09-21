using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Queries;

public class GetMeasurementUnitByIdQueryHandler(IMeasurementUnitRepositoryService repository)
    : IRequestHandler<GetMeasurementUnitByIdQuery, Result<MeasurementUnitDto>>
{
    public Task<Result<MeasurementUnitDto>> Handle(GetMeasurementUnitByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
