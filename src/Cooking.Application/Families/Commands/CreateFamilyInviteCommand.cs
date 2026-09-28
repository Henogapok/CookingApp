using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public record CreateFamilyInviteCommand(Guid FamilyId, Guid UserId) : IRequest<Result<FamilyInviteDto>>;
