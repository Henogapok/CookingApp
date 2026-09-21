using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Queries;

public record GetComplexitiesQuery : IRequest<Result<List<ReferenceEntityDto>>>;
