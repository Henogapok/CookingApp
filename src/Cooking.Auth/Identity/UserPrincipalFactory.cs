using System.Collections.Immutable;
using System.Security.Claims;
using Cooking.Application.Users;
using Cooking.Auth.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Identity;

/// <summary>
/// Claim — одно утверждение о пользователе («sub = 3f2a…», «name = Иван»). Из них состоят и сессия, и токены.
/// Здесь решается, какие утверждения куда попадают: в сессию сервера авторизации, в access token (его читает Api)
/// и в id token (его читает клиент, чтобы показать имя).
/// </summary>
public static class UserPrincipalFactory
{
    /// <summary>Сессия на самом сервере авторизации (cookie) — только Id и имя для страницы входа.</summary>
    public static ClaimsPrincipal CreateSession(UserDto user)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, FullName(user));
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Пользователь для токенов с запрошенными клиентом scope'ами; роли (<see cref="UserRoles"/>) — только в access token.</summary>
    public static ClaimsPrincipal CreateForTokens(UserDto user, IEnumerable<string> scopes, IEnumerable<string> roles)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, FullName(user));
        identity.SetClaim(Claims.GivenName, user.FirstName);
        identity.SetClaim(Claims.FamilyName, user.LastName);
        identity.SetClaim(AuthConstants.TelegramIdClaim, user.TelegramId.ToString());
        identity.SetClaims(UserRoles.ClaimType, roles.ToImmutableArray());

        identity.SetScopes(scopes);
        // Токен со scope'ом API адресован API (aud): Cooking.Api принимает только такие.
        if (identity.HasScope(AuthConstants.ApiScope))
            identity.SetResources(AuthConstants.ApiScope);

        identity.SetDestinations(GetDestinations);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Роли пользователя. Считаются при каждой выдаче и обновлении токенов: убрали Id из Admin:TelegramIds и перезапустили Auth
    /// (переменные окружения контейнера меняются только с перезапуском) — роль пропадёт с ближайшим обновлением access token.
    /// </summary>
    public static IReadOnlyList<string> GetRoles(UserDto user, AdminOptions admin) =>
        admin.TelegramIds.Contains(user.TelegramId) ? [UserRoles.Admin] : [];

    /// <summary>Id пользователя (claim sub) из сессии или из ранее выданного code/refresh-токена.</summary>
    public static Guid? GetUserId(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.GetClaim(Claims.Subject), out var id) ? id : null;

    /// <summary>Профиль попадает в id token, только если клиент запросил scope profile; access token получает всё.</summary>
    private static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],

        Claims.Name or Claims.GivenName or Claims.FamilyName or AuthConstants.TelegramIdClaim
            when claim.Subject!.HasScope(Scopes.Profile) => [Destinations.AccessToken, Destinations.IdentityToken],

        _ => [Destinations.AccessToken],
    };

    private static string FullName(UserDto user) =>
        user.LastName is null ? user.FirstName : $"{user.FirstName} {user.LastName}";
}
