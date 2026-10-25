namespace Cooking.Auth.Options;

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
