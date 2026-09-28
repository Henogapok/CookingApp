namespace Cooking.Bot;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Токен от @BotFather. Локально — User Secrets, в проде — переменная окружения Telegram__BotToken.</summary>
    public string? BotToken { get; set; }

    /// <summary>false — long polling (разработка), true — webhook (прод).</summary>
    public bool UseWebhook { get; set; }

    /// <summary>Полный публичный HTTPS-адрес webhook'а, например https://example.com/api/telegram/webhook.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Секрет, который Telegram присылает в заголовке X-Telegram-Bot-Api-Secret-Token —
    /// так webhook отличает настоящие запросы от Telegram от чужих.
    /// </summary>
    public string? WebhookSecretToken { get; set; }
}
