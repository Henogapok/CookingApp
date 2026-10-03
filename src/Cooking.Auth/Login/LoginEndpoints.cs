using Cooking.Application.Auth;
using Cooking.Application.Auth.Commands;
using Cooking.Application.Common.Errors;
using Cooking.Application.Users.Queries;
using Cooking.Auth.OpenIddict;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Cooking.Auth.Login;

/// <summary>
/// Страница входа и вход через бота. Результат входа — сессия сервера авторизации (cookie); дальше браузер
/// возвращается на /connect/authorize (returnUrl), и тот выдаёт клиенту code.
/// </summary>
public static class LoginEndpoints
{
    /// <summary>Секрет браузера для опроса запроса на вход (см. LoginRequest.PollTokenHash).</summary>
    private const string PollTokenCookie = "cooking.login";

    public static IEndpointRouteBuilder MapLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Redirect("/login"));

        endpoints.MapGet("/login", (IWebHostEnvironment environment) =>
            Results.File(Path.Combine(environment.WebRootPath, "login.html"), "text/html; charset=utf-8"));

        // Страница показывает «Вы вошли как …», если сессия уже есть.
        endpoints.MapGet("/login/session", (HttpContext context) =>
            context.User.Identity?.IsAuthenticated == true
                ? Results.Ok(new { name = context.User.Identity.Name })
                : Results.Unauthorized());

        endpoints.MapPost("/login/bot", StartBotLoginAsync);
        endpoints.MapPost("/login/bot/poll", PollBotLoginAsync);

        return endpoints;
    }

    /// <summary>Новый запрос на вход: ссылка на бота — странице, секрет для опроса — в HttpOnly cookie.</summary>
    private static async Task<IResult> StartBotLoginAsync(
        HttpContext context, ISender mediator, TelegramBotInfo botInfo, CancellationToken cancellationToken)
    {
        if (await botInfo.GetUsernameAsync(cancellationToken) is not { } botUsername)
            return Results.Problem("Вход через бота не настроен.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var result = await mediator.Send(new CreateLoginRequestCommand(), cancellationToken);
        if (result.IsFailed)
            return Results.Problem("Не удалось начать вход.");

        var request = result.Value;
        context.Response.Cookies.Append(PollTokenCookie, request.PollToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/login",
            Expires = request.ExpiresAt,
        });

        return Results.Ok(new { link = LoginRequestLink.Build(botUsername, request.Code), expiresAt = request.ExpiresAt });
    }

    /// <summary>
    /// Страница опрашивает раз в пару секунд, подтвердил ли пользователь вход в боте. Подтвердил — открываем сессию
    /// и говорим, куда вернуться (только на свой адрес: returnUrl приходит от браузера).
    /// </summary>
    private static async Task<IResult> PollBotLoginAsync(
        PollRequest body, HttpContext context, ISender mediator, IOptions<AuthOptions> options, CancellationToken cancellationToken)
    {
        if (context.Request.Cookies[PollTokenCookie] is not { Length: > 0 } pollToken)
            return Results.Ok(new { status = "expired" });

        var result = await mediator.Send(new CompleteLoginRequestCommand(pollToken), cancellationToken);
        if (result.IsFailed)
        {
            context.Response.Cookies.Delete(PollTokenCookie, new CookieOptions { Path = "/login" });
            return result.Errors.OfType<AppError>().Any(e => e.Code is ErrorCode.NotFound or ErrorCode.Validation)
                ? Results.Ok(new { status = "expired" })
                : Results.Problem("Не удалось проверить вход.");
        }

        switch (result.Value)
        {
            case { State: LoginRequestState.Pending }:
                return Results.Ok(new { status = "pending" });

            case { State: LoginRequestState.Confirmed, UserId: { } userId }:
                var user = await mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
                if (user.IsFailed)
                    return Results.Problem("Не удалось найти пользователя.");

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    AuthClaims.CreateSessionPrincipal(user.Value),
                    new AuthenticationProperties { IsPersistent = true });

                context.Response.Cookies.Delete(PollTokenCookie, new CookieOptions { Path = "/login" });
                return Results.Ok(new { status = "confirmed", redirect = SafeReturnUrl(body.ReturnUrl) });

            default:
                context.Response.Cookies.Delete(PollTokenCookie, new CookieOptions { Path = "/login" });
                return Results.Ok(new { status = "declined" });
        }
    }

    /// <summary>Только локальный путь ("/connect/authorize?..."), не "//evil.com" и не "https://evil.com" — защита от open redirect.</summary>
    public static string SafeReturnUrl(string? returnUrl) =>
        returnUrl is { Length: > 1 } && returnUrl[0] == '/' && returnUrl[1] != '/' && returnUrl[1] != '\\'
            ? returnUrl
            : "/login";

    public record PollRequest(string? ReturnUrl);
}
