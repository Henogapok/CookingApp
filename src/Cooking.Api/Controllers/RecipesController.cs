using Cooking.Api.Contracts;
using Cooking.Application.RecipeDrafts.Commands;
using Cooking.Application.Recipes.Commands;
using Cooking.Application.Recipes.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class RecipesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipesQuery(CurrentUserId, search), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipeByIdQuery(id, CurrentUserId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(RecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateRecipeCommand(CurrentUserId, request.ToFields()), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RecipeRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateRecipeCommand(id, CurrentUserId, request.ToFields()), cancellationToken));

    /// <summary>
    /// Изменить рецепт своими словами через LLM: создаёт черновик (202 + Id черновика), дальше —
    /// как с обычным черновиком: GET /api/RecipeDrafts/{draftId}, затем confirm обновит этот рецепт.
    /// </summary>
    [HttpPost("{id:guid}/edit-drafts")]
    public async Task<IActionResult> Edit(Guid id, CorrectRecipeDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new EditRecipeCommand(id, CurrentUserId, request.Text), cancellationToken);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(RecipeDraftsController.GetById), "RecipeDrafts", new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteRecipeCommand(id, CurrentUserId), cancellationToken));
}
