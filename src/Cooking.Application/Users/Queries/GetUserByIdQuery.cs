using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;
