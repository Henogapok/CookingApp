using Cooking.Auth.Seeding;
using Cooking.Infrastructure.Persistence;

namespace Cooking.Auth.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Перед запуском: миграции (если включены настройкой Database:MigrateOnStartup), затем клиенты OAuth — им нужны таблицы OpenIddict.</summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
            await app.Services.MigrateDatabaseAsync();

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OAuthClientSeeder>().SeedAsync(CancellationToken.None);
    }
}
