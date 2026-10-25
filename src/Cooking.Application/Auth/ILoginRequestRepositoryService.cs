using FluentResults;

namespace Cooking.Application.Auth;

/// <summary>Вход на сайт через бота — см. <see cref="Cooking.Domain.Entities.Users.LoginRequest"/>.</summary>
public interface ILoginRequestRepositoryService
{
    /// <summary>Новый запрос на вход; просроченные удаляются.</summary>
    Task<Result<LoginRequestCreatedDto>> CreateAsync(CancellationToken cancellationToken);

    /// <summary>Пользователь подтвердил вход в боте.</summary>
    Task<Result> ConfirmAsync(string code, Guid userId, CancellationToken cancellationToken);

    /// <summary>Пользователь отказался от входа в боте.</summary>
    Task<Result> DeclineAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Сайт опрашивает запрос по секрету браузера. Подтверждённый запрос отдаёт пользователя один раз
    /// и становится использованным; ожидающий — Pending. Нет / истёк / уже использован — NotFound.
    /// </summary>
    Task<Result<LoginRequestStatusDto>> CompleteAsync(string pollToken, CancellationToken cancellationToken);
}
