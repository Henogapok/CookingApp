using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Queries;

public record GetSourceTypesQuery : IRequest<Result<List<ReferenceEntityDto>>>;
