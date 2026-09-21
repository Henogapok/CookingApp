using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public record DeleteComplexityCommand(Guid Id) : IRequest<Result>;
