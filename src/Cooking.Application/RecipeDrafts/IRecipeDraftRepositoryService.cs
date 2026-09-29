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

    /// <summary>Для фонового разбора: исходный текст, автор, текущая версия и правка — без проверки доступа.</summary>
    Task<Result<RecipeDraftSource>> GetSourceAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Сохраняет новую версию черновика и снимает отметку о правке.</summary>
    Task<Result> SetContentAsync(Guid id, RecipeDraftContent content, CancellationToken cancellationToken);

    /// <summary>Запоминает правку. LogicConflict — черновик ещё разбирается или предыдущая правка не применена.</summary>
    Task<Result> StartCorrectionAsync(Guid id, Guid userId, string correction, CancellationToken cancellationToken);

    /// <summary>Для фонового разбора: правку применить не удалось — черновик остаётся прежним.</summary>
    Task ClearCorrectionAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<RecipeDraftContent>> GetContentAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Result<RecipeDraftDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Для фонового разбора: удалить черновик, который не удалось разобрать.</summary>
    Task DiscardAsync(Guid id, CancellationToken cancellationToken);
}

/// <param name="Content">null — черновик ещё не разобран.</param>
/// <param name="PendingCorrection">Правка, которую надо применить к Content.</param>
public record RecipeDraftSource(Guid Id, Guid UserId, string SourceText, RecipeDraftContent? Content, string? PendingCorrection);
