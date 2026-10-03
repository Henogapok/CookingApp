namespace Cooking.Auth.Options;

/// <summary>Секция Telegram: тот же бот, что и в Api (локально — dev-бот).</summary>
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Токен бота: нужен, чтобы узнать username для ссылки входа, а позже — проверять подпись данных Telegram Login Widget.
    /// Локально — User Secrets, в проде — переменная окружения Telegram__BotToken.
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>Username бота без @; если задан — getMe не вызывается.</summary>
    public string? BotUsername { get; set; }
}
