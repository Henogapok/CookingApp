using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Commands;

public record CreateFamilyCommand(Guid UserId, string Name) : IRequest<Result<Guid>>;
