using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public record CreateSourceTypeCommand(string Name) : IRequest<Result<Guid>>;
