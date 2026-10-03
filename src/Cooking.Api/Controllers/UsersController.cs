using Cooking.Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

/// <summary>Пользователи регистрируются сами — при входе через Telegram (бот или Cooking.Auth).</summary>
public class UsersController : BaseController
{
    /// <summary>Текущий пользователь (из токена): имя, семья.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetUserByIdQuery(CurrentUserId), cancellationToken));
}
