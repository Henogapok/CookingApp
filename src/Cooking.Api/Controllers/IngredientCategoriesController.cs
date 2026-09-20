using Cooking.Api.Contracts;
using Cooking.Application.ReferenceData.Commands;
using Cooking.Application.ReferenceData.Queries;
using Cooking.Domain.Entities.Ingredients;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class IngredientCategoriesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetReferenceEntitiesQuery<IngredientCategory>(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetReferenceEntityByIdQuery<IngredientCategory>(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(ReferenceEntityRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateReferenceEntityCommand<IngredientCategory>(request.Name), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ReferenceEntityRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateReferenceEntityCommand<IngredientCategory>(id, request.Name), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteReferenceEntityCommand<IngredientCategory>(id), cancellationToken));
}
