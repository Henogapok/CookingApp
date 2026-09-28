using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public record UpdateTagCommand(Guid Id, string Name, Guid TagTypeId) : IRequest<Result>;
