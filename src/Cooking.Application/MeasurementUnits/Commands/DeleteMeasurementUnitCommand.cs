using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Commands;

public record DeleteMeasurementUnitCommand(Guid Id) : IRequest<Result>;
