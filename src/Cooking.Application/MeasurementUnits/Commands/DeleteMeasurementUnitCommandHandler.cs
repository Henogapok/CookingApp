using Cooking.Application.Common.Errors;
using Cooking.Application.Common.Interfaces;
using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class DeleteMeasurementUnitCommandHandler(IDataContext dataContext)
    : IRequestHandler<DeleteMeasurementUnitCommand, Result>
{
    public async Task<Result> Handle(DeleteMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await dataContext.MeasurementUnits.FindAsync([request.Id], cancellationToken);

        if (unit is null)
            return Result.Fail(new AppError($"MeasurementUnit with id '{request.Id}' was not found.", ErrorCode.NotFound));

        dataContext.MeasurementUnits.Remove(unit);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
