using System.Threading.Channels;
using Cooking.Application.RecipeDrafts;

namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>
/// Очередь в памяти процесса (singleton). Задачи, не успевшие обработаться до перезапуска, теряются —
/// черновик просто истечёт, пользователь пришлёт текст ещё раз. Нужна надёжность — заменить на брокер.
/// </summary>
public class InProcessRecipeParsingQueue : IRecipeParsingQueue
{
    private readonly Channel<RecipeParsingJob> _channel = Channel.CreateUnbounded<RecipeParsingJob>();

    public ChannelReader<RecipeParsingJob> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(RecipeParsingJob job, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(job, cancellationToken);
}
