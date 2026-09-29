using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Users;

namespace Cooking.Domain.Entities.Recipes;

/// <summary>
/// Черновик рецепта, распознанного LLM из текста. Живёт между «разобрал» и «пользователь подтвердил»:
/// в Recipe и каталог ингредиентов ничего не попадает, пока черновик не сохранён.
/// </summary>
public class RecipeDraft : BaseEntity
{
    /// <summary>Черновик виден только тому, кто его создал.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Исходный текст — нужен для разбора и для правок черновика. У черновика из видео сначала пуст (или содержит
    /// описание, присланное подписью к видео), а после скачивания и расшифровки — «описание + расшифровка».
    /// </summary>
    public required string SourceText { get; set; }

    /// <summary>Ссылка на видео (Reels), из которого сделан черновик; уходит в Recipe.SourceUrl.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Присланный пользователем видеофайл, который ещё предстоит расшифровать (удаляется после этого).</summary>
    public string? MediaFilePath { get; set; }

    /// <summary>false — текст ещё надо получить из видео (скачать по SourceUrl или расшифровать MediaFilePath).</summary>
    public bool IsSourceLoaded { get; set; } = true;

    /// <summary>Разобранный рецепт (JSON, jsonb в Postgres). null — разбор ещё идёт.</summary>
    public string? ContentJson { get; set; }

    /// <summary>
    /// Правка пользователя, которая сейчас применяется в фоне. Не null — черновик нельзя сохранить
    /// и нельзя править повторно, пока не придёт новая версия.
    /// </summary>
    public string? PendingCorrection { get; set; }

    /// <summary>После этого момента черновик нельзя сохранить; просроченные удаляются.</summary>
    public DateTime ExpiresAt { get; set; }
}
