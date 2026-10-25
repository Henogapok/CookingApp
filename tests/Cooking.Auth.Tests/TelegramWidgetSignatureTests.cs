using System.Security.Cryptography;
using System.Text;
using Cooking.Auth.Identity;

namespace Cooking.Auth.Tests;

public class TelegramWidgetSignatureTests
{
    private const string BotToken = "123456:TEST-token";
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Данные, как их подписал бы Telegram: hash по алгоритму из документации.</summary>
    private static Dictionary<string, string> Signed(DateTimeOffset authDate, string token = BotToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["id"] = "566833795",
            ["first_name"] = "Валерий",
            ["last_name"] = "Апрелев",
            ["username"] = "valeriy",
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
        };

        var checkString = string.Join('\n', fields.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value}"));
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        fields["hash"] = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(checkString))).ToLowerInvariant();
        return fields;
    }

    [Fact]
    public void Verify_ValidData_ReturnsUser()
    {
        var result = TelegramWidgetSignature.Verify(Signed(Now.AddMinutes(-1)), BotToken, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TelegramWidgetUser(566833795, "Валерий", "Апрелев"), result.Value);
    }

    [Fact]
    public void Verify_ChangedField_Fails()
    {
        var fields = Signed(Now);
        fields["id"] = "1";

        Assert.True(TelegramWidgetSignature.Verify(fields, BotToken, Now).IsFailed);
    }

    [Fact]
    public void Verify_SignedWithAnotherBot_Fails()
    {
        var fields = Signed(Now, token: "999:another-bot");

        Assert.True(TelegramWidgetSignature.Verify(fields, BotToken, Now).IsFailed);
    }

    [Fact]
    public void Verify_OutdatedAuthDate_Fails()
    {
        var fields = Signed(Now - TelegramWidgetSignature.MaxAge - TimeSpan.FromMinutes(1));

        Assert.True(TelegramWidgetSignature.Verify(fields, BotToken, Now).IsFailed);
    }

    [Fact]
    public void Verify_AuthDateFromFuture_Fails()
    {
        Assert.True(TelegramWidgetSignature.Verify(Signed(Now.AddHours(1)), BotToken, Now).IsFailed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-hex")]
    [InlineData("abcd")]
    public void Verify_MissingOrMalformedHash_Fails(string? hash)
    {
        var fields = Signed(Now);
        if (hash is null)
            fields.Remove("hash");
        else
            fields["hash"] = hash;

        Assert.True(TelegramWidgetSignature.Verify(fields, BotToken, Now).IsFailed);
    }
}
