using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FluentResults;

namespace Cooking.Auth.Identity;

/// <summary>Пользователь, которого подтвердил Telegram Login Widget.</summary>
public record TelegramWidgetUser(long TelegramId, string FirstName, string? LastName);

/// <summary>
/// Проверка данных Telegram Login Widget (https://core.telegram.org/widgets/login#checking-authorization).
/// Данные приходят от браузера, поэтому верить им можно только по подписи: Telegram считает
/// hash = HMAC-SHA256(строка проверки, ключ = SHA256(токен бота)). Токен знают только Telegram и мы —
/// подделать hash, не зная токена, нельзя.
/// </summary>
public static class TelegramWidgetSignature
{
    /// <summary>Данные старше — отвергаем: перехваченный ответ виджета нельзя использовать для входа бесконечно.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(1);

    // Часы сервера и Telegram могут немного расходиться — auth_date «из будущего» в пределах минуты допустим.
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    private const int MaxFields = 16;
    private const int MaxValueLength = 512;

    public static Result<TelegramWidgetUser> Verify(IReadOnlyDictionary<string, string> fields, string botToken, DateTimeOffset now)
    {
        if (fields.Count > MaxFields || fields.Values.Any(v => v.Length > MaxValueLength))
            return Result.Fail("Too many or too long fields.");

        if (!fields.TryGetValue("hash", out var hash) || !TryParseHex(hash, out var actual))
            return Result.Fail("No valid hash.");

        // Строка проверки: все поля, кроме hash, «ключ=значение», по алфавиту ключей, через \n.
        // Берём ровно то, что пришло: Telegram может добавить новые поля, и они тоже подписаны.
        var checkString = string.Join('\n', fields
            .Where(f => f.Key != "hash")
            .OrderBy(f => f.Key, StringComparer.Ordinal)
            .Select(f => $"{f.Key}={f.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var expected = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(checkString));

        // Сравнение за постоянное время: по времени ответа нельзя подобрать hash побайтно.
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return Result.Fail("Hash mismatch.");

        if (!fields.TryGetValue("auth_date", out var authDateText)
            || !long.TryParse(authDateText, NumberStyles.None, CultureInfo.InvariantCulture, out var authDateSeconds))
            return Result.Fail("No auth_date.");

        var age = now - DateTimeOffset.FromUnixTimeSeconds(authDateSeconds);
        if (age > MaxAge || age < -ClockSkew)
            return Result.Fail("auth_date is outdated.");

        if (!fields.TryGetValue("id", out var idText)
            || !long.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var telegramId)
            || !fields.TryGetValue("first_name", out var firstName) || string.IsNullOrWhiteSpace(firstName))
            return Result.Fail("No id or first_name.");

        var lastName = fields.TryGetValue("last_name", out var last) && !string.IsNullOrWhiteSpace(last) ? last : null;
        return Result.Ok(new TelegramWidgetUser(telegramId, firstName, lastName));
    }

    private static bool TryParseHex(string value, out byte[] bytes)
    {
        bytes = [];
        // SHA-256 = 32 байта = 64 hex-символа.
        if (value.Length != 64)
            return false;

        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
