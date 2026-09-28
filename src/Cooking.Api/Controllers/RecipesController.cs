using Cooking.Api.Contracts;
using Cooking.Application.Recipes.Commands;
using Cooking.Application.Recipes.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

// userId в query/теле — временно, пока нет авторизации через Telegram Login Widget.
public class RecipesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid userId, [FromQuery] string? search, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipesQuery(userId, search), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetRecipeByIdQuery(id, userId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(RecipeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateRecipeCommand(request.UserId, request.ToFields()), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value, userId = request.UserId }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RecipeRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateRecipeCommand(id, request.UserId, request.ToFields()), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteRecipeCommand(id, userId), cancellationToken));
}
