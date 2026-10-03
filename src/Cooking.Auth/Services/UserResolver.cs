using System.Security.Claims;
using Cooking.Application.Users;
using Cooking.Application.Users.Queries;
using Cooking.Auth.Identity;
using MediatR;

namespace Cooking.Auth.Services;

/// <summary>
/// Пользователь из базы по сессии или по ранее выданному code/refresh-токену (claim sub). Перечитываем каждый раз:
/// удалённому пользователю новые токены не выдаём, а смена имени попадает в следующий токен.
/// </summary>
public class UserResolver(ISender mediator)
{
    /// <summary>null — в principal нет пользователя (не вошёл) или его уже нет в базе.</summary>
    public async Task<UserDto?> ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(principal) is not { } userId)
            return null;

        var result = await mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }
}
