using Cooking.Api.Contracts;
using Cooking.Application.ReferenceData.IngredientCategories.Commands;
using Cooking.Application.ReferenceData.IngredientCategories.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class IngredientCategoriesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIngredientCategoriesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIngredientCategoryByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(ReferenceEntityRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateIngredientCategoryCommand(request.Name), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ReferenceEntityRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateIngredientCategoryCommand(id, request.Name), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteIngredientCategoryCommand(id), cancellationToken));
}
