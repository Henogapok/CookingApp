using FluentResults;
using MediatR;

namespace Cooking.Application.Tags.Commands;

public record DeleteTagCommand(Guid Id) : IRequest<Result>;
