using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public record UpdateTagTypeCommand(Guid Id, string Name) : IRequest<Result>;
