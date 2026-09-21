using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public record UpdateSourceTypeCommand(Guid Id, string Name) : IRequest<Result>;
