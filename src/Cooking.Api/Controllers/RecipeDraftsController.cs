using Cooking.Api.Contracts;
using Cooking.Application.RecipeDrafts.Commands;
using Cooking.Application.RecipeDrafts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

/// <summary>
/// Разбор рецепта из текста — те же команды, что у бота. Разбор асинхронный: POST возвращает Id черновика,
/// дальше клиент опрашивает GET (409 — ещё разбирается, 404 — не рецепт / ошибка / истёк).
/// </summary>
public class RecipeDraftsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateRecipeDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateRecipeDraftCommand(request.UserId, request.Text), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetById), new { id = result.Value, userId = request.UserId }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipeDraftQuery(id, userId), cancellationToken));

    /// <summary>Подставить оценки ИИ для порций/времени, не указанных в тексте.</summary>
    [HttpPost("{id:guid}/estimates")]
    public async Task<IActionResult> ApplyEstimates(Guid id, RecipeDraftActorRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new ApplyRecipeDraftEstimatesCommand(id, request.UserId), cancellationToken));

    /// <summary>Сохранить черновик как рецепт; возвращает Id рецепта.</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, RecipeDraftActorRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new ConfirmRecipeDraftCommand(id, request.UserId), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new CancelRecipeDraftCommand(id, userId), cancellationToken));
}
