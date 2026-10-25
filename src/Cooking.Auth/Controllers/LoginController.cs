using Cooking.Application.Common.Errors;
using Cooking.Auth.Contracts;
using Cooking.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Auth.Controllers;

/// <summary>
/// Страница входа и вход через бота. Итог входа — сессия сервера авторизации (cookie); после неё браузер
/// возвращается на /connect/authorize (returnUrl), и тот выдаёт клиенту code.
/// </summary>
[Route("login")]
public class LoginController(BotLoginService botLogin, IWebHostEnvironment environment) : Controller
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

        // returnUrl приходит от браузера — возвращаем только на свой адрес (защита от open redirect).
        var redirect = Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : "/login";
        return Ok(new BotLoginPollResponse("confirmed", redirect));
    }
}
