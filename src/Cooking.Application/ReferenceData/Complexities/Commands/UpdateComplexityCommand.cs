using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public record UpdateComplexityCommand(Guid Id, string Name) : IRequest<Result>;
