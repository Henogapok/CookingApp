using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Queries;

public record GetTagByIdQuery(Guid Id) : IRequest<Result<TagDto>>;
