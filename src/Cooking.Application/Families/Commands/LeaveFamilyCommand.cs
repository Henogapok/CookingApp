using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public record LeaveFamilyCommand(Guid FamilyId, Guid UserId) : IRequest<Result>;
