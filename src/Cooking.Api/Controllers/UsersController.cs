using Cooking.Api.Contracts;
using Cooking.Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

/// <summary>Пользователи регистрируются сами — при входе через Telegram (бот или Cooking.Auth).</summary>
public class UsersController : BaseController
{
    /// <summary>Текущий пользователь (из токена): имя, семья, тема PWA.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetUserByIdQuery(CurrentUserId), cancellationToken));

    /// <summary>Тема, выбранная в PWA: сохраняется в профиле, чтобы совпадала на всех устройствах.</summary>
    [HttpPut("me/theme")]
    public async Task<IActionResult> SetTheme(UserThemeRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(request.ToCommand(CurrentUserId), cancellationToken));
}
