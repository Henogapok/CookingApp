using FluentResults;

namespace Cooking.Application.Users;

public interface IUserRepositoryService
{
    /// <summary>
    /// Идемпотентная регистрация по TelegramId (вызывается на каждый /start): создаёт пользователя
    /// без семьи или обновляет имя уже существующего.
    /// </summary>
    Task<Result<UserDto>> RegisterAsync(long telegramId, string firstName, string? lastName, CancellationToken cancellationToken);
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<UserDto>> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken);
    Task<Result<List<UserDto>>> GetAllAsync(CancellationToken cancellationToken);
}
