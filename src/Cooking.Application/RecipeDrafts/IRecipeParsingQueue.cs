namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Очередь фонового разбора черновиков. Сейчас — в памяти процесса; задача — простая запись,
/// чтобы при необходимости её можно было отправлять через брокер (RabbitMQ), не меняя вызывающий код.
/// </summary>
public interface IRecipeParsingQueue
{
    ValueTask EnqueueAsync(RecipeParsingJob job, CancellationToken cancellationToken);
}

public record RecipeParsingJob(Guid DraftId);
