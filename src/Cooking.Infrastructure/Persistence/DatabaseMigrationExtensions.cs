using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cooking.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    /// <summary>Прод: применяет недостающие миграции при старте Api (включается настройкой Database:MigrateOnStartup).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CookingDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
