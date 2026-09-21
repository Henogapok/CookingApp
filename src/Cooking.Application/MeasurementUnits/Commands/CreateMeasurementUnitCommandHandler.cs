using Cooking.Application.Common.Interfaces;
using Cooking.Domain.Entities.Ingredients;
using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class CreateMeasurementUnitCommandHandler(IDataContext dataContext)
    : IRequestHandler<CreateMeasurementUnitCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = new MeasurementUnit
        {
            Name = request.Name,
            Abbreviation = request.Abbreviation,
        };

        dataContext.MeasurementUnits.Add(unit);
        await dataContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(unit.Id);
    }
}
