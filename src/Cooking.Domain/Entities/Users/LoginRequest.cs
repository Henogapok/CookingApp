using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Users;

/// <summary>
/// Вход на сайт через бота (по схеме OAuth Device Flow). Сайт создаёт запрос и показывает ссылку
/// t.me/&lt;bot&gt;?start=login_&lt;Code&gt;; пользователь подтверждает вход в боте, а сайт забирает результат
/// по PollToken, который есть только у браузера, начавшего вход. Code виден (он в ссылке) — по нему
/// можно только подтвердить вход, но не войти.
/// </summary>
public class LoginRequest : BaseEntity
{
    /// <summary>Публичный код из ссылки на бота (start-параметр: ≤ 64 символов [A-Za-z0-9_-]).</summary>
    public required string Code { get; set; }

    /// <summary>SHA-256 секрета браузера; сам секрет хранится только в cookie браузера.</summary>
    public required string PollTokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Кто подтвердил вход в боте; null — ещё не подтверждён.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Пользователь нажал «Отмена» в боте.</summary>
    public DateTime? DeclinedAt { get; set; }

    /// <summary>Сайт забрал результат и выполнил вход — запрос одноразовый.</summary>
    public DateTime? CompletedAt { get; set; }
}
