using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Cooking.Api.Bot;

/// <summary>Прод: при старте сообщает Telegram, куда слать апдейты.</summary>
public class BotWebhookRegistrationService(
    ITelegramBotClient bot,
    IOptions<TelegramOptions> options,
    ILogger<BotWebhookRegistrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.WebhookUrl) || string.IsNullOrWhiteSpace(settings.WebhookSecretToken))
            throw new InvalidOperationException(
                "Telegram:WebhookUrl and Telegram:WebhookSecretToken must be configured when Telegram:UseWebhook is true.");

        await bot.SetWebhook(
            settings.WebhookUrl,
            allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery],
            secretToken: settings.WebhookSecretToken,
            cancellationToken: cancellationToken);

        logger.LogInformation("Telegram webhook registered at {WebhookUrl}", settings.WebhookUrl);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
