using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Cooking.Auth.Login;

/// <summary>Username бота для ссылки t.me/&lt;bot&gt;?start=… — из настроек или один раз через getMe.</summary>
public class TelegramBotInfo(IHttpClientFactory httpClientFactory, IOptions<TelegramOptions> options, ILogger<TelegramBotInfo> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _username;

    /// <summary>null — бот не настроен (нет ни username, ни токена) или Telegram недоступен.</summary>
    public async Task<string?> GetUsernameAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!string.IsNullOrWhiteSpace(settings.BotUsername))
            return settings.BotUsername;

        if (_username is not null || string.IsNullOrWhiteSpace(settings.BotToken))
            return _username;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_username is not null)
                return _username;

            // URL содержит токен: HttpClient логирует его на Information — категория System заглушена до Warning в appsettings.
            var response = await httpClientFactory.CreateClient()
                .GetFromJsonAsync<GetMeResponse>($"https://api.telegram.org/bot{settings.BotToken}/getMe", cancellationToken);

            _username = response?.Result?.Username;
            return _username;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Telegram getMe failed: {Status}", ex.StatusCode);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private record GetMeResponse([property: JsonPropertyName("result")] GetMeUser? Result);

    private record GetMeUser([property: JsonPropertyName("username")] string? Username);
}
