namespace Cooking.Auth.Login;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Токен того же бота, что и в Api (локально — dev-бот): нужен, чтобы узнать username для ссылки входа,
    /// а позже — проверять подпись данных Telegram Login Widget.
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>Username бота без @; если задан — getMe не вызывается.</summary>
    public string? BotUsername { get; set; }
}
