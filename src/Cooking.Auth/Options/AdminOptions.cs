namespace Cooking.Auth.Options;

/// <summary>
/// Секция Admin: кто администратор. Админ один-два и меняются почти никогда, поэтому список — в конфиге, а не в базе.
/// Локально — User Secrets, в проде — переменная окружения Admin__TelegramIds__0.
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Telegram Id администраторов: при выдаче токена они получают роль admin.</summary>
    public long[] TelegramIds { get; set; } = [];
}
