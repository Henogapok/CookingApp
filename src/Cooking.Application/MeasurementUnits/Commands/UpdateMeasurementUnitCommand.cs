using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public record UpdateMeasurementUnitCommand(Guid Id, string Name, string Abbreviation) : IRequest<Result>;
