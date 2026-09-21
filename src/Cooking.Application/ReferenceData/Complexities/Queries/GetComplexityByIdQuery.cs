using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Queries;

public record GetComplexityByIdQuery(Guid Id) : IRequest<Result<ReferenceEntityDto>>;
