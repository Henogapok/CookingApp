using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class DeleteMeasurementUnitCommandHandler(IMeasurementUnitRepositoryService repository)
    : IRequestHandler<DeleteMeasurementUnitCommand, Result>
{
    public Task<Result> Handle(DeleteMeasurementUnitCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
