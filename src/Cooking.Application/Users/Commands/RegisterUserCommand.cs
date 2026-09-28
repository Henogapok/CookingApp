using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Commands;

public record RegisterUserCommand(long TelegramId, string FirstName, string? LastName) : IRequest<Result<UserDto>>;
