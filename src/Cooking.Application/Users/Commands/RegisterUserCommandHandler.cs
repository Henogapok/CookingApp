using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Commands;

public class RegisterUserCommandHandler(IUserRepositoryService repository)
    : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        repository.RegisterAsync(request.TelegramId, request.FirstName, request.LastName, cancellationToken);
}
