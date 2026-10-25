using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

public class DeclineLoginRequestCommandHandler(ILoginRequestRepositoryService repository)
    : IRequestHandler<DeclineLoginRequestCommand, Result>
{
    public Task<Result> Handle(DeclineLoginRequestCommand request, CancellationToken cancellationToken) =>
        repository.DeclineAsync(request.Code, cancellationToken);
}
