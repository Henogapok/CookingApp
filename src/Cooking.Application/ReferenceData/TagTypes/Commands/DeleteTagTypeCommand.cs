using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public record DeleteTagTypeCommand(Guid Id) : IRequest<Result>;
