using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

public class CreateLoginRequestCommandHandler(ILoginRequestRepositoryService repository)
    : IRequestHandler<CreateLoginRequestCommand, Result<LoginRequestCreatedDto>>
{
    public Task<Result<LoginRequestCreatedDto>> Handle(CreateLoginRequestCommand request, CancellationToken cancellationToken) =>
        repository.CreateAsync(cancellationToken);
}
