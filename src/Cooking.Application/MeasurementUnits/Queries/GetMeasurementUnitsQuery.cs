using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Queries;

public record GetMeasurementUnitsQuery : IRequest<Result<List<MeasurementUnitDto>>>;
