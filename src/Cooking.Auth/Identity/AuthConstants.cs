namespace Cooking.Auth.Identity;

public static class AuthConstants
{
    /// <summary>Scope и audience API: токен с этим scope'ом адресован Cooking.Api (aud = cooking-api).</summary>
    public const string ApiScope = "cooking-api";

    /// <summary>Свой claim: Telegram Id пользователя (стандартных sub, name и т.п. для этого нет).</summary>
    public const string TelegramIdClaim = "telegram_id";
}
