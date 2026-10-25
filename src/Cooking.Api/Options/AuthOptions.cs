namespace Cooking.Api.Options;

/// <summary>Секция Auth: где Cooking.Auth и какие токены принимать.</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Адрес Cooking.Auth (issuer токенов); null — авторизация не настроена, все защищённые эндпоинты отвечают 401.</summary>
    public string? Authority { get; set; }

    /// <summary>Api принимает только токены, адресованные ему (aud).</summary>
    public string Audience { get; set; } = "cooking-api";

    /// <summary>Клиент, под которым Swagger UI входит через Cooking.Auth.</summary>
    public string SwaggerClientId { get; set; } = "cooking-swagger";
}
