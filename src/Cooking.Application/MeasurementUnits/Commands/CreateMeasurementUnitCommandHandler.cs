using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public class CreateMeasurementUnitCommandHandler(IMeasurementUnitRepositoryService repository)
    : IRequestHandler<CreateMeasurementUnitCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateMeasurementUnitCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(request.Name, request.Abbreviation, cancellationToken);
}
