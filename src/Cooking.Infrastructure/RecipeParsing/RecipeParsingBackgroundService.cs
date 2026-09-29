using Cooking.Application.RecipeDrafts.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>
/// Разбирает черновики из <see cref="InProcessRecipeParsingQueue"/> — до MaxParallelism одновременно.
/// Вся логика — в ParseRecipeDraftCommand; здесь только транспорт, как у бота.
/// </summary>
public class RecipeParsingBackgroundService(
    InProcessRecipeParsingQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<RecipeParsingOptions> options,
    ILogger<RecipeParsingBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Parallel.ForEachAsync(
            queue.Reader.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, options.Value.MaxParallelism),
                CancellationToken = stoppingToken,
            },
            async (job, ct) =>
            {
                // Scope на задачу — свой DbContext, как у HTTP-запроса или апдейта бота.
                await using var scope = scopeFactory.CreateAsyncScope();
                try
                {
                    var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                        .Send(new ParseRecipeDraftCommand(job.DraftId), ct);

                    if (result.IsFailed)
                        logger.LogWarning("Recipe draft {DraftId} was not parsed: {Errors}",
                            job.DraftId, string.Join("; ", result.Errors.Select(e => e.Message)));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Одна упавшая задача не должна останавливать очередь.
                    logger.LogError(ex, "Failed to parse recipe draft {DraftId}", job.DraftId);
                }
            });
}
