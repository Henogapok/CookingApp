namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Сообщает пользователю, чем закончился фоновый разбор. Сейчас реализация — Telegram-бот;
/// для PWA появится своя (например, SignalR), команды при этом не меняются.
/// </summary>
public interface IRecipeDraftNotifier
{
    Task DraftReadyAsync(Guid draftId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Блюд больше, чем разбираем за раз: пользователь выбирает до RecipeDraftLimits.MaxDishes из списка
    /// (SelectRecipeDraftDishesCommand), после чего придут превью выбранных.
    /// </summary>
    Task DishChoiceRequiredAsync(Guid draftId, Guid userId, IReadOnlyList<string> dishes, CancellationToken cancellationToken);

    /// <summary>Первый разбор не удался — черновика больше нет.</summary>
    Task DraftFailedAsync(Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken);

    /// <summary>Правку применить не удалось — черновик остался прежним.</summary>
    Task DraftCorrectionFailedAsync(Guid draftId, Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken);
}

public enum RecipeDraftFailureReason
{
    /// <summary>В тексте не нашлось рецепта.</summary>
    NotARecipe,

    /// <summary>Разбор не настроен (нет ключа LLM).</summary>
    ParserUnavailable,

    /// <summary>LLM не ответил или ответил ошибкой.</summary>
    ParserError,

    /// <summary>Видео по ссылке не скачалось — можно попросить прислать сам файл.</summary>
    VideoUnavailable,

    /// <summary>В видео нет речи, а в описании — текста (например, только музыка).</summary>
    NoTextInVideo,
}
