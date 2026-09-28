using FluentResults;
using MediatR;

namespace Cooking.Application.Users.Queries;

public class GetUserByIdQueryHandler(IUserRepositoryService repository)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
