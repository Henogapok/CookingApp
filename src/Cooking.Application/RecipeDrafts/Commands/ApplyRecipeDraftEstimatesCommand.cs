using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>Подставить оценки ИИ вместо порций/времени, которых не было в тексте. Повторный вызов ничего не меняет.</summary>
public record ApplyRecipeDraftEstimatesCommand(Guid DraftId, Guid UserId) : IRequest<Result>;
