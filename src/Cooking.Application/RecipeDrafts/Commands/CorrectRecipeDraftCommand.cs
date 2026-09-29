using FluentResults;
using MediatR;

namespace Cooking.Application.RecipeDrafts.Commands;

/// <summary>
/// Правка разобранного черновика своими словами («лука не надо, порций 4»). Применяется в фоне через LLM;
/// пока правка не применена, черновик нельзя сохранить. Результат придёт через IRecipeDraftNotifier.
/// </summary>
public record CorrectRecipeDraftCommand(Guid DraftId, Guid UserId, string Text) : IRequest<Result>;
