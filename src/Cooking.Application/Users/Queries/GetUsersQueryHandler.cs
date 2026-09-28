using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public class GetUsersQueryHandler(IUserRepositoryService repository)
    : IRequestHandler<GetUsersQuery, Result<List<UserDto>>>
{
    public Task<Result<List<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
