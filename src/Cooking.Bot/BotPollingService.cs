using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Cooking.Bot;

/// <summary>
/// Режим разработки: бот сам опрашивает Telegram (long polling), публичный HTTPS-адрес не нужен.
/// </summary>
public class BotPollingService(
    ITelegramBotClient bot,
    IServiceScopeFactory scopeFactory,
    ILogger<BotPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan ErrorRetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Если у бота остался webhook (например, токен раньше использовался в проде), getUpdates не работает.
            await bot.DeleteWebhook(cancellationToken: stoppingToken);
            await bot.SetMyCommands(BotCommandNames.All, cancellationToken: stoppingToken);
            var me = await bot.GetMe(stoppingToken);
            logger.LogInformation("Telegram bot @{BotUsername} started in polling mode", me.Username);

            await bot.ReceiveAsync(
                updateHandler: async (_, update, ct) =>
                {
                    // Scope на каждый апдейт — как HTTP-запрос: свой DbContext и MediatR-хендлеры.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    try
                    {
                        await scope.ServiceProvider.GetRequiredService<BotUpdateHandler>().HandleAsync(update, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Failed to handle Telegram update {UpdateId}", update.Id);
                    }
                },
                errorHandler: async (_, ex, ct) =>
                {
                    logger.LogError(ex, "Telegram polling error, retrying in {Delay}", ErrorRetryDelay);
                    await Task.Delay(ErrorRetryDelay, ct);
                },
                receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery] },
                cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Не роняем весь Api, если бот не стартовал (неверный токен, нет сети) — REST продолжит работать.
            logger.LogError(ex, "Telegram bot polling stopped");
        }
    }
}
