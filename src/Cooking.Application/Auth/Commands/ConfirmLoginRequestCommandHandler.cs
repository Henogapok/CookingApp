using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

public class ConfirmLoginRequestCommandHandler(ILoginRequestRepositoryService repository)
    : IRequestHandler<ConfirmLoginRequestCommand, Result>
{
    public Task<Result> Handle(ConfirmLoginRequestCommand request, CancellationToken cancellationToken) =>
        repository.ConfirmAsync(request.Code, request.UserId, cancellationToken);
}
