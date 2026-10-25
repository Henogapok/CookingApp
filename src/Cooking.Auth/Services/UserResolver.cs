using System.Security.Claims;
using Cooking.Application.Users;
using Cooking.Auth.Identity;

namespace Cooking.Auth.Services;

/// <summary>
/// Пользователь из базы по сессии или по ранее выданному code/refresh-токену (claim sub). Перечитываем каждый раз:
/// удалённому пользователю новые токены не выдаём, а смена имени попадает в следующий токен.
/// </summary>
public class UserResolver(IUserRepositoryService users, ILogger<UserResolver> logger)
{
    /// <summary>null — в principal нет пользователя (не вошёл) или его уже нет в базе.</summary>
    public async Task<UserDto?> ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(principal) is not { } userId)
            return null;

        var result = await users.GetByIdAsync(userId, cancellationToken);
        if (result.IsFailed)
        {
            logger.LogWarning("User {UserId} from session/token can't be loaded: {Errors}", userId, result.Errors);
            return null;
        }

        return result.Value;
    }
}
