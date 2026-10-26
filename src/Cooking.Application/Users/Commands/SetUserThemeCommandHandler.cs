using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Commands;

public class SetUserThemeCommandHandler(IUserRepositoryService repository)
    : IRequestHandler<SetUserThemeCommand, Result>
{
    public Task<Result> Handle(SetUserThemeCommand request, CancellationToken cancellationToken) =>
        repository.SetThemeAsync(request.UserId, request.Theme, cancellationToken);
}
