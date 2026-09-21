using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public record DeleteSourceTypeCommand(Guid Id) : IRequest<Result>;
