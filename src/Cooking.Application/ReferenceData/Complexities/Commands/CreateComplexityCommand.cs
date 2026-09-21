using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public record CreateComplexityCommand(string Name) : IRequest<Result<Guid>>;
