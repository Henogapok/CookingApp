using System.Security.Claims;
using Cooking.Application.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.OpenIddict;

/// <summary>Какие данные о пользователе попадают в сессию сервера и в токены.</summary>
public static class AuthClaims
{
    public const string TelegramId = "telegram_id";

    /// <summary>Сессия на самом сервере авторизации (cookie) — только Id и имя для страницы входа.</summary>
    public static ClaimsPrincipal CreateSessionPrincipal(UserDto user)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, FullName(user));
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Id пользователя из сессии или из ранее выданного кода/refresh-токена (claim sub).</summary>
    public static Guid? GetUserId(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.GetClaim(Claims.Subject), out var id) ? id : null;

    /// <summary>
    /// Пользователь для токенов. Данные берём из базы при каждой выдаче (и при обновлении по refresh-токену),
    /// поэтому смена имени попадает в следующий токен.
    /// </summary>
    public static ClaimsPrincipal CreateTokenPrincipal(UserDto user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, FullName(user));
        identity.SetClaim(Claims.GivenName, user.FirstName);
        identity.SetClaim(Claims.FamilyName, user.LastName);
        identity.SetClaim(TelegramId, user.TelegramId.ToString());

        identity.SetScopes(scopes);
        // Токен со scope'ом API адресован API (aud): Cooking.Api принимает только такие.
        if (identity.HasScope(AuthOptions.ApiScope))
            identity.SetResources(AuthOptions.ApiScope);

        identity.SetDestinations(GetDestinations);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Куда идёт каждый claim: access token читает API, id token — клиент (показать имя).
    /// Профиль попадает в id token, только если клиент запросил scope profile.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],

        Claims.Name or Claims.GivenName or Claims.FamilyName or TelegramId when claim.Subject!.HasScope(Scopes.Profile) =>
            [Destinations.AccessToken, Destinations.IdentityToken],

        _ => [Destinations.AccessToken],
    };

    private static string FullName(UserDto user) =>
        user.LastName is null ? user.FirstName : $"{user.FirstName} {user.LastName}";
}
