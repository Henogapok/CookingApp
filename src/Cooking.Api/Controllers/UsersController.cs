using Cooking.Api.Contracts;
using Cooking.Application.Users.Commands;
using Cooking.Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class UsersController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetUsersQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpGet("by-telegram/{telegramId:long}")]
    public async Task<IActionResult> GetByTelegramId(long telegramId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetUserByTelegramIdQuery(telegramId), cancellationToken));

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(
            new RegisterUserCommand(request.TelegramId, request.FirstName, request.LastName),
            cancellationToken));
}
