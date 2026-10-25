using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cooking.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    /// <summary>Любое число, одинаковое у всех сервисов: ключ advisory lock'а Postgres на время миграции.</summary>
    private const long MigrationLockKey = 0x436F6F6B696E67; // "Cooking"

    /// <summary>
    /// Прод: применяет недостающие миграции при старте (включается настройкой Database:MigrateOnStartup).
    /// Api и Auth стартуют одновременно и оба мигрируют, а EF 8 не защищает от параллельного запуска —
    /// поэтому на время миграции берём advisory lock: второй сервис дождётся первого и увидит, что всё применено.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CookingDbContext>();

        // Lock живёт в сессии Postgres, поэтому держим одно соединение: EF выполнит миграции через него же.
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({MigrationLockKey})", cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({MigrationLockKey})", CancellationToken.None);
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
