using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Queries;

public record GetTagTypesQuery : IRequest<Result<List<ReferenceEntityDto>>>;
