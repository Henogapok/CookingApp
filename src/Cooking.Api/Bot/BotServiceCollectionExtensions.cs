using Telegram.Bot;

namespace Cooking.Api.Bot;

public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TelegramOptions.SectionName);
        services.Configure<TelegramOptions>(section);

        var options = section.Get<TelegramOptions>() ?? new TelegramOptions();

        // Без токена (CI, чужая машина без User Secrets) Api поднимается без бота — REST работает как раньше.
        if (string.IsNullOrWhiteSpace(options.BotToken))
            return services;

        services.AddHttpClient(nameof(TelegramBotClient))
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(options.BotToken, httpClient));

        services.AddSingleton<BotInfoProvider>();
        services.AddScoped<BotUpdateHandler>();

        if (options.UseWebhook)
            services.AddHostedService<BotWebhookRegistrationService>();
        else
            services.AddHostedService<BotPollingService>();

        return services;
    }
}
