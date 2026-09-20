using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class UpdateMeasurementUnitCommandHandler(IDataContext dataContext)
    : IRequestHandler<UpdateMeasurementUnitCommand, Result>
{
    public async Task<Result> Handle(UpdateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([request.Id], cancellationToken);

        if (unit is null)
            return Result.Fail(new NotFoundError($"MeasurementUnit with id '{request.Id}' was not found."));

        unit.Name = request.Name;
        unit.Abbreviation = request.Abbreviation;
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
