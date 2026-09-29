using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// В тексте оказалось больше блюд, чем разбираем за раз: пользователь выбирает нужные (номера в списке, с нуля).
/// Разбор выбранных идёт в фоне, превью придут через IRecipeDraftNotifier.
/// </summary>
public record SelectRecipeDraftDishesCommand(Guid DraftId, Guid UserId, List<int> DishIndexes) : IRequest<Result>;
