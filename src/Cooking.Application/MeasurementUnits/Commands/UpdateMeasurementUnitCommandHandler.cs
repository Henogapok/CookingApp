using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class UpdateMeasurementUnitCommandHandler(IMeasurementUnitRepositoryService repository)
    : IRequestHandler<UpdateMeasurementUnitCommand, Result>
{
    public Task<Result> Handle(UpdateMeasurementUnitCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(request.Id, request.Name, request.Abbreviation, cancellationToken);
}
