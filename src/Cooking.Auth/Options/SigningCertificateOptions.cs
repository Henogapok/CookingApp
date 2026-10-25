namespace Cooking.Auth.Options;

/// <summary>
/// Секция Auth:Certificates — сертификаты, которыми OpenIddict подписывает токены (JWT) и шифрует коды/refresh-токены.
/// Ключи должны переживать перезапуск: новый ключ = все выданные токены недействительны, пользователей выкидывает.
/// </summary>
public class SigningCertificateOptions
{
    /// <summary>Путь к .pfx сертификата подписи (в проде — на docker-volume). Нет файла — создаётся при старте.</summary>
    public string? SigningPath { get; set; }

    /// <summary>Путь к .pfx сертификата шифрования. Нет файла — создаётся при старте.</summary>
    public string? EncryptionPath { get; set; }

    /// <summary>Пароль .pfx; можно не задавать — файлы и так лежат на volume, доступном только контейнеру.</summary>
    public string? Password { get; set; }

    public bool IsConfigured => SigningPath is { Length: > 0 } && EncryptionPath is { Length: > 0 };
}
