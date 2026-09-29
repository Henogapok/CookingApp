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

    /// <summary>Исходный текст — нужен для разбора и для будущих правок черновика.</summary>
    public required string SourceText { get; set; }

    /// <summary>Разобранный рецепт (JSON, jsonb в Postgres). null — разбор ещё идёт.</summary>
    public string? ContentJson { get; set; }

    /// <summary>После этого момента черновик нельзя сохранить; просроченные удаляются.</summary>
    public DateTime ExpiresAt { get; set; }
}
