using Cooking.Application.Users;
using Cooking.Application.Users.Queries;
using Cooking.Auth.OpenIddict;
using MediatR;
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
/// делает OpenIddict; здесь только решение «кто этот пользователь и что положить в токен».
/// </summary>
public class AuthorizationController(ISender mediator) : Controller
{
    /// <summary>
    /// Начало входа: клиент (PWA, Swagger) перенаправляет сюда браузер. Нет сессии — отправляем на страницу входа,
    /// она вернёт сюда же. Сессия есть — сразу выдаём code (это и есть SSO: второй клиент входит без логина).
    /// </summary>
    [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // prompt=login — клиент просит войти заново, даже если сессия есть.
        var forceLogin = request.HasPromptValue(PromptValues.Login);
        if (forceLogin)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var session = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var user = session.Succeeded && !forceLogin
            ? await FindUserAsync(AuthClaims.GetUserId(session.Principal), HttpContext.RequestAborted)
            : null;

        if (user is null)
        {
            // prompt=none — клиент проверяет сессию «тихо» и просит не показывать страницу входа.
            if (request.HasPromptValue(PromptValues.None))
                return Forbid(Errors.LoginRequired, "The user is not logged in.");

            // Вернуться сюда же с теми же параметрами, но без prompt — иначе prompt=login зациклится.
            var parameters = (Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                .Where(p => p.Key != Parameters.Prompt);

            return Challenge(
                new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters) },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return SignIn(AuthClaims.CreateTokenPrincipal(user, request.GetScopes()), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Обмен code (+ code_verifier PKCE) или refresh-токена на токены. OpenIddict уже проверил code/refresh-токен
    /// и достал из него пользователя; мы перечитываем его из базы — удалённому пользователю новые токены не выдаём.
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

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = await FindUserAsync(AuthClaims.GetUserId(result.Principal), HttpContext.RequestAborted);
        if (user is null)
            return Forbid(Errors.InvalidGrant, "The user no longer exists.");

        return SignIn(AuthClaims.CreateTokenPrincipal(user, result.Principal!.GetScopes()), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Выход: закрывает сессию сервера (то есть во всех клиентах сразу) и возвращает на post_logout_redirect_uri.</summary>
    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Без post_logout_redirect_uri (или с незарегистрированным) OpenIddict вернёт на страницу входа.
        return SignOut(new AuthenticationProperties { RedirectUri = "/login" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<UserDto?> FindUserAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id)
            return null;

        var result = await mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? result.Value : null;
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
