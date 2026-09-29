using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Фоновый разбор черновика через LLM. Вызывается обработчиком очереди, не пользователем:
/// итог (готово / не рецепт / ошибка) уходит пользователю через IRecipeDraftNotifier.
/// </summary>
public record ParseRecipeDraftCommand(Guid DraftId) : IRequest<Result>;
