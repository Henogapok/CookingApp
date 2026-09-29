using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Изменение сохранённого рецепта своими словами («порций 6, добавь чеснок»): создаёт черновик из текущего рецепта
/// и сразу применяет к нему правку тем же механизмом, что и CorrectRecipeDraftCommand. Сохранение такого черновика
/// обновляет исходный рецепт. Возвращает Id черновика; превью придёт через IRecipeDraftNotifier.
/// Доступ — как на редактирование рецепта: автор и его семья.
/// </summary>
public record EditRecipeCommand(Guid RecipeId, Guid UserId, string Text) : IRequest<Result<Guid>>;
