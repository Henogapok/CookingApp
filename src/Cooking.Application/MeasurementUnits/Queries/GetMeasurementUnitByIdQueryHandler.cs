using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Queries;

public class GetMeasurementUnitByIdQueryHandler(IDataContext dataContext)
    : IRequestHandler<GetMeasurementUnitByIdQuery, Result<MeasurementUnitDto>>
{
    public async Task<Result<MeasurementUnitDto>> Handle(GetMeasurementUnitByIdQuery request, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([request.Id], cancellationToken);

        return unit is null
            ? Result.Fail(new NotFoundError($"MeasurementUnit with id '{request.Id}' was not found."))
            : Result.Ok(new MeasurementUnitDto(unit.Id, unit.Name, unit.Abbreviation));
    }
}
