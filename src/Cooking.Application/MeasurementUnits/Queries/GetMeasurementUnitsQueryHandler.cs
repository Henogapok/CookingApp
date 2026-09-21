using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Queries;

public class GetMeasurementUnitsQueryHandler(IMeasurementUnitRepositoryService repository)
    : IRequestHandler<GetMeasurementUnitsQuery, Result<List<MeasurementUnitDto>>>
{
    public Task<Result<List<MeasurementUnitDto>>> Handle(GetMeasurementUnitsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
