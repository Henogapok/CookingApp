using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public class LeaveFamilyCommandHandler(IFamilyRepositoryService repository)
    : IRequestHandler<LeaveFamilyCommand, Result>
{
    public Task<Result> Handle(LeaveFamilyCommand request, CancellationToken cancellationToken) =>
        repository.LeaveAsync(request.FamilyId, request.UserId, cancellationToken);
}
