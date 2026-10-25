namespace Cooking.Application.Auth;

/// <summary>Ссылка на бота для входа на сайт: t.me/&lt;bot&gt;?start=login_&lt;Code&gt;, бот получает "/start login_&lt;Code&gt;".</summary>
public static class LoginRequestLink
{
    /// <summary>Коды приглашений в семью не содержат ни '_', ни 'l' — с ними не пересекается.</summary>
    public const string StartPrefix = "login_";

    public static string Build(string botUsername, string code) => $"https://t.me/{botUsername}?start={StartPrefix}{code}";

    /// <summary>Код входа из start-параметра; null — это не вход (например, приглашение в семью).</summary>
    public static string? ParseCode(string startPayload) =>
        startPayload.StartsWith(StartPrefix, StringComparison.Ordinal) && startPayload.Length > StartPrefix.Length
            ? startPayload[StartPrefix.Length..]
            : null;
}
