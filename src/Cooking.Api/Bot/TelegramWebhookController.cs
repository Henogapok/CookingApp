using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Cooking.Api.Bot;

/// <summary>Прод: Telegram присылает сюда каждый апдейт POST-запросом.</summary>
[ApiController]
[Route("api/telegram")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TelegramWebhookController(
    BotUpdateHandler handler,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (!IsFromTelegram())
            return Unauthorized();

        // Update читаем сами с настройками сериализации Telegram.Bot (snake_case и т.д.),
        // чтобы не менять глобальные JSON-настройки MVC для остальных контроллеров.
        var update = await JsonSerializer.DeserializeAsync<Update>(Request.Body, JsonBotAPI.Options, cancellationToken);
        if (update is null)
            return BadRequest();

        try
        {
            await handler.HandleAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            // Всегда отвечаем 200: на ошибку Telegram будет бесконечно повторять тот же апдейт.
            logger.LogError(ex, "Failed to handle Telegram update {UpdateId}", update.Id);
        }

        return Ok();
    }

    private bool IsFromTelegram()
    {
        var expected = options.Value.WebhookSecretToken;
        if (string.IsNullOrEmpty(expected) || !Request.Headers.TryGetValue(SecretTokenHeader, out var actual))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual.ToString()),
            Encoding.UTF8.GetBytes(expected));
    }
}
