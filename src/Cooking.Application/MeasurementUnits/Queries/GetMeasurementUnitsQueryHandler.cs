using Cooking.Application.Common.Interfaces;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.MeasurementUnits.Queries;

public class GetMeasurementUnitsQueryHandler(IDataContext dataContext)
    : IRequestHandler<GetMeasurementUnitsQuery, Result<List<MeasurementUnitDto>>>
{
    public async Task<Result<List<MeasurementUnitDto>>> Handle(
        GetMeasurementUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var units = await dataContext.MeasurementUnits
            .OrderBy(u => u.Name)
            .Select(u => new MeasurementUnitDto(u.Id, u.Name, u.Abbreviation))
            .ToListAsync(cancellationToken);

        return Result.Ok(units);
    }
}
