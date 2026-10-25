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
        var result = await Mediator.Send(new CreateRecipeDraftCommand(CurrentUserId, request.Text), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    /// <summary>Черновик из ссылки на Instagram Reels: скачивание, расшифровка и разбор — в фоне, дальше как с текстом.</summary>
    [HttpPost("from-url")]
    public async Task<IActionResult> CreateFromUrl(CreateRecipeDraftFromUrlRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateRecipeDraftFromUrlCommand(CurrentUserId, request.Url), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipeDraftQuery(id, CurrentUserId), cancellationToken));

    /// <summary>Подставить оценки ИИ для порций/времени, не указанных в тексте.</summary>
    [HttpPost("{id:guid}/estimates")]
    public async Task<IActionResult> ApplyEstimates(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new ApplyRecipeDraftEstimatesCommand(id, CurrentUserId), cancellationToken));

    /// <summary>
    /// Правка своими словами («лука не надо, порций 4»). Применяется в фоне: 202, затем GET отдаёт новую версию
    /// (IsBeingCorrected = false). Пока правка применяется, confirm отвечает 409.
    /// </summary>
    [HttpPost("{id:guid}/corrections")]
    public async Task<IActionResult> Correct(Guid id, CorrectRecipeDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CorrectRecipeDraftCommand(id, CurrentUserId, request.Text), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetById), new { id }, null)
            : HandleResult(result);
    }

    /// <summary>
    /// В тексте больше блюд, чем разбирается за раз: выбрать нужные (номера в списке, с нуля). Разбор — в фоне,
    /// каждое блюдо становится отдельным черновиком.
    /// </summary>
    [HttpPost("{id:guid}/dishes")]
    public async Task<IActionResult> SelectDishes(Guid id, SelectRecipeDraftDishesRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SelectRecipeDraftDishesCommand(id, CurrentUserId, request.DishIndexes), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetById), new { id }, null)
            : HandleResult(result);
    }

    /// <summary>Сохранить черновик как рецепт; возвращает Id рецепта.</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new ConfirmRecipeDraftCommand(id, CurrentUserId), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new CancelRecipeDraftCommand(id, CurrentUserId), cancellationToken));
}
