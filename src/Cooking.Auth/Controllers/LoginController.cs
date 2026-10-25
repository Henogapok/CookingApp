using Cooking.Application.Common.Errors;
using Cooking.Auth.Contracts;
using Cooking.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Auth.Controllers;

/// <summary>
/// Страница входа: вход через бота и через Telegram Login Widget. Итог входа — сессия сервера авторизации (cookie); после неё браузер
/// возвращается на /connect/authorize (returnUrl), и тот выдаёт клиенту code.
/// </summary>
[Route("login")]
public class LoginController(
    BotLoginService botLogin,
    TelegramWidgetLoginService widgetLogin,
    TelegramBotInfo botInfo,
    IWebHostEnvironment environment) : Controller
{
    /// <summary>Секрет браузера для опроса запроса на вход (см. LoginRequest.PollTokenHash).</summary>
    private const string PollTokenCookie = "cooking.login";

    [HttpGet("~/")]
    public IActionResult Root() => Redirect("/login");

    [HttpGet]
    public IActionResult Page() =>
        PhysicalFile(Path.Combine(environment.WebRootPath, "login.html"), "text/html; charset=utf-8");

    /// <summary>Страница показывает «Вы вошли как …», если сессия уже есть.</summary>
    [HttpGet("session")]
    public IActionResult Session() =>
        User.Identity is { IsAuthenticated: true, Name: { } name } ? Ok(new LoginSessionResponse(name)) : Unauthorized();

    /// <summary>Какие способы входа показать: кнопку виджета — только если он включён и известен username бота.</summary>
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var username = widgetLogin.IsEnabled ? await botInfo.GetUsernameAsync(cancellationToken) : null;
        return Ok(new LoginOptionsResponse(username));
    }

    /// <summary>
    /// Вход через виджет: Telegram уже подтвердил пользователя и передал странице подписанные данные,
    /// мы проверяем подпись и сразу открываем сессию — без опроса, как у входа через бота.
    /// </summary>
    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramWidgetLogin([FromBody] TelegramWidgetLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await widgetLogin.SignInAsync(request.Data, cancellationToken);
        if (result.IsFailed)
        {
            var code = result.Errors.OfType<AppError>().Select(e => (ErrorCode?)e.Code).FirstOrDefault();
            return code switch
            {
                ErrorCode.Validation => Problem("Telegram не подтвердил вход. Попробуйте ещё раз.", statusCode: StatusCodes.Status401Unauthorized),
                ErrorCode.Unavailable => Problem("Вход через Telegram не настроен.", statusCode: StatusCodes.Status503ServiceUnavailable),
                _ => Problem("Не удалось войти."),
            };
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Value,
            new AuthenticationProperties { IsPersistent = true });

        return Ok(new LoginCompletedResponse(SafeRedirect(request.ReturnUrl)));
    }

    /// <summary>Новый запрос на вход: ссылка на бота — странице, секрет для опроса — в HttpOnly cookie.</summary>
    [HttpPost("bot")]
    public async Task<IActionResult> StartBotLogin(CancellationToken cancellationToken)
    {
        var result = await botLogin.StartAsync(cancellationToken);
        if (result.IsFailed)
        {
            var unavailable = result.Errors.OfType<AppError>().Any(e => e.Code == ErrorCode.Unavailable);
            return Problem(unavailable ? "Вход через бота не настроен." : "Не удалось начать вход.",
                statusCode: unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError);
        }

        Response.Cookies.Append(PollTokenCookie, result.Value.PollToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/login",
            Expires = result.Value.ExpiresAt,
        });

        return Ok(new BotLoginStartResponse(result.Value.BotLink, result.Value.ExpiresAt));
    }

    /// <summary>Страница опрашивает раз в пару секунд, подтвердил ли пользователь вход в боте.</summary>
    [HttpPost("bot/poll")]
    public async Task<IActionResult> PollBotLogin([FromBody] BotLoginPollRequest request, CancellationToken cancellationToken)
    {
        var result = await botLogin.PollAsync(Request.Cookies[PollTokenCookie], cancellationToken);
        if (result.IsFailed)
            return Problem("Не удалось проверить вход.");

        var poll = result.Value;
        if (poll.Status != BotLoginStatus.Pending)
            Response.Cookies.Delete(PollTokenCookie, new CookieOptions { Path = "/login" });

        if (poll is not { Status: BotLoginStatus.Confirmed, Session: { } session })
            return Ok(new BotLoginPollResponse(poll.Status.ToString().ToLowerInvariant()));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, session,
            new AuthenticationProperties { IsPersistent = true });

        return Ok(new BotLoginPollResponse("confirmed", SafeRedirect(request.ReturnUrl)));
    }

    /// <summary>returnUrl приходит от браузера — возвращаем только на свой адрес (защита от open redirect).</summary>
    private string SafeRedirect(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl : "/login";
}
