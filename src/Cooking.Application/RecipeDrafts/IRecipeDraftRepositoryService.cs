using FluentResults;

namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Черновик виден только автору; чужой или просроченный — NotFound.
/// Черновик, который ещё разбирается, — LogicConflict.
/// </summary>
public interface IRecipeDraftRepositoryService
{
    /// <summary>Создаёт черновик (ещё не разобранный) и заодно удаляет просроченные.</summary>
    Task<Result<Guid>> CreateAsync(Guid userId, string sourceText, CancellationToken cancellationToken);

    /// <summary>Для фонового разбора: исходный текст и автор, без проверки доступа.</summary>
    Task<Result<RecipeDraftSource>> GetSourceAsync(Guid id, CancellationToken cancellationToken);

    Task<Result> SetContentAsync(Guid id, RecipeDraftContent content, CancellationToken cancellationToken);

    Task<Result<RecipeDraftContent>> GetContentAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Result<RecipeDraftDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Для фонового разбора: удалить черновик, который не удалось разобрать.</summary>
    Task DiscardAsync(Guid id, CancellationToken cancellationToken);
}

public record RecipeDraftSource(Guid Id, Guid UserId, string SourceText);
