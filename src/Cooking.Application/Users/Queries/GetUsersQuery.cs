using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public record GetUsersQuery : IRequest<Result<List<UserDto>>>;
