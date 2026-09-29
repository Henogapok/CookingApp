using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Пользователь подтвердил черновик: создаёт недостающие ингредиенты (помечены как созданные LLM),
/// сохраняет рецепт (новый — или обновляет исходный, если это черновик изменения) и удаляет черновик.
/// Возвращает Id рецепта.
/// </summary>
public record ConfirmRecipeDraftCommand(Guid DraftId, Guid UserId) : IRequest<Result<Guid>>;
