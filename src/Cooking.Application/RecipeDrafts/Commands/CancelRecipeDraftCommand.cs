using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

public record CancelRecipeDraftCommand(Guid DraftId, Guid UserId) : IRequest<Result>;
