using FluentResults;
using MediatR;

namespace Cooking.Application.MeasurementUnits.Queries;

public record GetMeasurementUnitByIdQuery(Guid Id) : IRequest<Result<MeasurementUnitDto>>;
