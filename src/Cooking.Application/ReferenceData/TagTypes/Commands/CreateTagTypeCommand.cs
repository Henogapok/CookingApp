using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public record CreateTagTypeCommand(string Name) : IRequest<Result<Guid>>;
