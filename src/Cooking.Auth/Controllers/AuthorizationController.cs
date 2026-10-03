using Cooking.Auth.Identity;
using Cooking.Auth.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cooking.Auth.Controllers;

/// <summary>
/// Эндпоинты OAuth 2.0 / OpenID Connect. Протокол (проверка клиента, redirect_uri, PKCE, выпуск и подпись токенов)
/// делает OpenIddict до того, как запрос дойдёт сюда; контроллер только отвечает, кто пользователь.
/// </summary>
public class AuthorizationController(UserResolver users) : Controller
{
    /// <summary>
    /// Начало входа: клиент (PWA, Swagger) перенаправляет сюда браузер. Нет сессии — отправляем на страницу входа,
    /// она вернёт сюда же. Сессия есть — сразу выдаём code (это и есть SSO: второй клиент входит без логина).
    /// GET и POST — по стандарту (OpenID Connect Core, 3.1.2.1): параметры могут прийти и в строке, и формой.
    /// </summary>
    [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // prompt=login — клиент просит войти заново, даже если сессия есть.
        if (request.HasPromptValue(PromptValues.Login))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToLogin();
        }

        var session = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var user = await users.ResolveAsync(session.Principal, HttpContext.RequestAborted);

        if (user is not null)
            return SignIn(UserPrincipalFactory.CreateForTokens(user, request.GetScopes()), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // prompt=none — клиент проверяет сессию «тихо» и просит не показывать страницу входа.
        return request.HasPromptValue(PromptValues.None)
            ? Forbid(Errors.LoginRequired, "The user is not logged in.")
            : RedirectToLogin();
    }

    /// <summary>
    /// Обмен code (+ code_verifier PKCE) или refresh-токена на токены. OpenIddict уже проверил code/refresh-токен
    /// и достал из него пользователя — перечитываем его из базы и выпускаем свежие токены.
    /// </summary>
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            throw new InvalidOperationException("The specified grant type is not supported.");

        var granted = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = await users.ResolveAsync(granted.Principal, HttpContext.RequestAborted);

        return user is null
            ? Forbid(Errors.InvalidGrant, "The user no longer exists.")
            : SignIn(UserPrincipalFactory.CreateForTokens(user, granted.Principal!.GetScopes()), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Выход: закрывает сессию сервера — то есть во всех клиентах сразу — и возвращает на post_logout_redirect_uri.
    /// GET и POST — по стандарту (OpenID Connect RP-Initiated Logout).
    /// </summary>
    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Без post_logout_redirect_uri (или с незарегистрированным) OpenIddict вернёт на страницу входа.
        return SignOut(new AuthenticationProperties { RedirectUri = "/login" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>На страницу входа с возвратом сюда же — с теми же параметрами, но без prompt (иначе prompt=login зациклится).</summary>
    private ChallengeResult RedirectToLogin()
    {
        var parameters = (Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
            .Where(p => p.Key != Parameters.Prompt);

        return Challenge(
            new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters) },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private ForbidResult Forbid(string error, string description) =>
        Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
            }),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
