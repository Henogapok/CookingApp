using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public record GetUserByTelegramIdQuery(long TelegramId) : IRequest<Result<UserDto>>;
