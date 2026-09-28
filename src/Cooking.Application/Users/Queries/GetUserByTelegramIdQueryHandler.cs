using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public class GetUserByTelegramIdQueryHandler(IUserRepositoryService repository)
    : IRequestHandler<GetUserByTelegramIdQuery, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(GetUserByTelegramIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
}
