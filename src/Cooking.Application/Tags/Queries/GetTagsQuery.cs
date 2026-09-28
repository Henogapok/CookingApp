using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Queries;

public record GetTagsQuery(Guid? TagTypeId) : IRequest<Result<List<TagDto>>>;
