using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace Cooking.Bot;

/// <summary>Кэширует username бота — он нужен для deep link'ов t.me/&lt;username&gt;?start=...</summary>
public class BotInfoProvider(IServiceScopeFactory scopeFactory)
{
    private string? _username;

    public async Task<string> GetUsernameAsync(CancellationToken cancellationToken)
    {
        if (_username is not null)
            return _username;

        await using var scope = scopeFactory.CreateAsyncScope();
        var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var me = await bot.GetMe(cancellationToken);

        return _username = me.Username!;
    }
}
