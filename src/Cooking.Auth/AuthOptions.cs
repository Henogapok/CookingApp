namespace Cooking.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Scope и audience API: токен с этим scope'ом принимает Cooking.Api (aud = cooking-api).</summary>
    public const string ApiScope = "cooking-api";

    /// <summary>Адрес сервера в поле iss токенов; null — берётся из запроса (локально).</summary>
    public string? Issuer { get; set; }

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>Сколько живёт сессия на самом сервере авторизации (cookie) — пока она есть, клиенты входят без логина (SSO).</summary>
    public int SessionLifetimeDays { get; set; } = 30;

    /// <summary>Клиенты (PWA, Swagger) — при старте создаются или обновляются в базе.</summary>
    public List<AuthClientOptions> Clients { get; set; } = [];
}

/// <summary>Публичный клиент (без секрета: SPA, Swagger) — Authorization Code + PKCE.</summary>
public class AuthClientOptions
{
    public string ClientId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    /// <summary>Куда можно вернуть code после входа — только точное совпадение.</summary>
    public List<string> RedirectUris { get; set; } = [];

    /// <summary>Куда можно вернуть пользователя после выхода.</summary>
    public List<string> PostLogoutRedirectUris { get; set; } = [];
}
