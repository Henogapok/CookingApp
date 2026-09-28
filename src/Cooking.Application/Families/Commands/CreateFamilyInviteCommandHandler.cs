using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public class CreateFamilyInviteCommandHandler(IFamilyRepositoryService repository)
    : IRequestHandler<CreateFamilyInviteCommand, Result<FamilyInviteDto>>
{
    public Task<Result<FamilyInviteDto>> Handle(CreateFamilyInviteCommand request, CancellationToken cancellationToken) =>
        repository.CreateInviteAsync(request.FamilyId, request.UserId, cancellationToken);
}
