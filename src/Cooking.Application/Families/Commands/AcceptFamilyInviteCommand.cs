using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public record AcceptFamilyInviteCommand(string Code, Guid UserId) : IRequest<Result<Guid>>;
