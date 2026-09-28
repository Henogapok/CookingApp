using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public record CreateTagCommand(string Name, Guid TagTypeId) : IRequest<Result<Guid>>;
