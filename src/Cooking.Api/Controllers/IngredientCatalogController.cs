using Cooking.Api.Contracts;
using Cooking.Application.Ingredients.Commands;
using Cooking.Application.Ingredients.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class IngredientCatalogController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIngredientCatalogsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetIngredientCatalogByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(IngredientCatalogRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateIngredientCatalogCommand(
                request.Name,
                request.CategoryId,
                request.BaseUnitId,
                request.PricePer100G,
                request.CaloriesPer100G,
                request.ProteinPer100G,
                request.FatPer100G,
                request.CarbsPer100G,
                request.CreatedBySourceId,
                request.NutritionSourceId),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, IngredientCatalogRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(
            new UpdateIngredientCatalogCommand(
                id,
                request.Name,
                request.CategoryId,
                request.BaseUnitId,
                request.PricePer100G,
                request.CaloriesPer100G,
                request.ProteinPer100G,
                request.FatPer100G,
                request.CarbsPer100G,
                request.CreatedBySourceId,
                request.NutritionSourceId),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteIngredientCatalogCommand(id), cancellationToken));
}
