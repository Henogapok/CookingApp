using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public class AcceptFamilyInviteCommandHandler(IFamilyRepositoryService repository)
    : IRequestHandler<AcceptFamilyInviteCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(AcceptFamilyInviteCommand request, CancellationToken cancellationToken) =>
        repository.AcceptInviteAsync(request.Code, request.UserId, cancellationToken);
}
