namespace Cooking.Auth.Options;

/// <summary>Секция Auth: время жизни токенов и сессии, клиенты.</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Адрес сервера в поле iss токенов; null — берётся из запроса (локально).</summary>
    public string? Issuer { get; set; }

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>Сколько живёт сессия на самом сервере авторизации (cookie) — пока она есть, клиенты входят без логина (SSO).</summary>
    public int SessionLifetimeDays { get; set; } = 30;

    /// <summary>Клиенты (PWA, Swagger) — при старте синхронизируются с базой (см. OAuthClientSeeder).</summary>
    public List<AuthClientOptions> Clients { get; set; } = [];
}
