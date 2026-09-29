using Cooking.Application.RecipeDrafts;

namespace Cooking.Infrastructure.RecipeParsing;

/// <summary>
/// Уведомления по умолчанию — никаких: Api без бота (нет токена) всё равно умеет разбирать черновики,
/// а клиент узнаёт результат, запрашивая черновик. Бот регистрирует свою реализацию поверх этой.
/// </summary>
public class NullRecipeDraftNotifier : IRecipeDraftNotifier
{
    public Task DraftReadyAsync(Guid draftId, Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DraftFailedAsync(Guid userId, RecipeDraftFailureReason reason, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
