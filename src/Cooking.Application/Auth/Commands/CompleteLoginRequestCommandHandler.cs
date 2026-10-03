using FluentResults;
using MediatR;

namespace Cooking.Application.Auth.Commands;

public class CompleteLoginRequestCommandHandler(ILoginRequestRepositoryService repository)
    : IRequestHandler<CompleteLoginRequestCommand, Result<LoginRequestStatusDto>>
{
    public Task<Result<LoginRequestStatusDto>> Handle(CompleteLoginRequestCommand request, CancellationToken cancellationToken) =>
        repository.CompleteAsync(request.PollToken, cancellationToken);
}
