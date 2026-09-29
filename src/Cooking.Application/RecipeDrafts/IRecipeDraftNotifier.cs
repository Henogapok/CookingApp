namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Сообщает пользователю, чем закончился фоновый разбор. Сейчас реализация — Telegram-бот;
/// для PWA появится своя (например, SignalR), команды при этом не меняются.
/// </summary>
public interface IRecipeDraftNotifier
{
    Task DraftReadyAsync(Guid draftId, Guid userId, CancellationToken cancellationToken);

    Task DraftFailedAsync(Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken);
}

public enum RecipeDraftFailureReason
{
    /// <summary>В тексте не нашлось рецепта.</summary>
    NotARecipe,

    /// <summary>Разбор не настроен (нет ключа LLM).</summary>
    ParserUnavailable,

    /// <summary>LLM не ответил или ответил ошибкой.</summary>
    ParserError,
}
