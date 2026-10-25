using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cooking.Auth.Identity;

/// <summary>
/// Сертификат ключей OpenIddict из .pfx-файла. Файла нет (первый запуск на сервере) — создаётся самоподписанный и сохраняется,
/// дальше при каждом старте читается тот же. Самоподписанного достаточно: никто не проверяет цепочку до центра сертификации,
/// Api доверяет ключу, потому что берёт его из /.well-known/jwks нашего же Auth.
/// </summary>
public static class CertificateFile
{
    // OpenIddict не принимает просроченные сертификаты: за 5 лет файл нужно удалить, тогда при старте создастся новый
    // (пользователям придётся войти заново).
    private const int LifetimeYears = 5;

    public static X509Certificate2 LoadOrCreate(string path, string? password, string subject, X509KeyUsageFlags usage)
    {
        if (!File.Exists(path))
            Create(path, password, subject, usage);

        // EphemeralKeySet: закрытый ключ только в памяти процесса, без копий в хранилище ключей ОС.
        return new X509Certificate2(path, password, X509KeyStorageFlags.EphemeralKeySet);
    }

    private static void Create(string path, string? password, string subject, X509KeyUsageFlags usage)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        // OpenIddict проверяет назначение: для подписи нужен DigitalSignature, для шифрования — KeyEncipherment.
        request.CertificateExtensions.Add(new X509KeyUsageExtension(usage, critical: true));

        var now = DateTimeOffset.UtcNow;
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(LifetimeYears));

        if (Path.GetDirectoryName(path) is { Length: > 0 } directory)
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }
}
