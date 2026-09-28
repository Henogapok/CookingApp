using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Cooking.Bot;

/// <summary>Прод: Telegram присылает сюда каждый апдейт POST-запросом.</summary>
public static class TelegramWebhookEndpoint
{
    public const string Route = "/api/telegram/webhook";
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>Маршрут появляется только в webhook-режиме и при настроенном токене.</summary>
    public static IEndpointRouteBuilder MapTelegramWebhook(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;

        if (options.UseWebhook && !string.IsNullOrWhiteSpace(options.BotToken))
            endpoints.MapPost(Route, HandleAsync).ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        [FromServices] BotUpdateHandler handler,
        [FromServices] IOptions<TelegramOptions> options,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!IsFromTelegram(request, options.Value.WebhookSecretToken))
            return Results.Unauthorized();

        // Update читаем сами с настройками сериализации Telegram.Bot (snake_case и т.д.),
        // чтобы не менять глобальные JSON-настройки для остальных эндпоинтов Api.
        var update = await JsonSerializer.DeserializeAsync<Update>(request.Body, JsonBotAPI.Options, cancellationToken);
        if (update is null)
            return Results.BadRequest();

        try
        {
            await handler.HandleAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            // Всегда отвечаем 200: на ошибку Telegram будет бесконечно повторять тот же апдейт.
            loggerFactory.CreateLogger(typeof(TelegramWebhookEndpoint))
                .LogError(ex, "Failed to handle Telegram update {UpdateId}", update.Id);
        }

        return Results.Ok();
    }

    private static bool IsFromTelegram(HttpRequest request, string? expected)
    {
        if (string.IsNullOrEmpty(expected) || !request.Headers.TryGetValue(SecretTokenHeader, out var actual))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual.ToString()),
            Encoding.UTF8.GetBytes(expected));
    }
}
