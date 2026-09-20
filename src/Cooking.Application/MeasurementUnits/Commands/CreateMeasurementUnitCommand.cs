using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public record CreateMeasurementUnitCommand(string Name, string Abbreviation) : IRequest<Result<Guid>>;
