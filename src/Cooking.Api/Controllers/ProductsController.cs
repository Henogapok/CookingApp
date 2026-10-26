using Cooking.Api.Contracts;
using Cooking.Application.Products.Commands;
using Cooking.Application.Products.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

/// <summary>Продукты семьи — что покупаете, с ценой и КБЖУ с упаковки; по ним считаются рецепты у того, кто смотрит.</summary>
public class ProductsController : BaseController
{
    /// <summary>Ингредиенты из рецептов семьи и с продуктами семьи — для экрана «Мои продукты» и выбора продукта в рецепте.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetProductsQuery(CurrentUserId), cancellationToken));

    /// <summary>Новый продукт сразу становится выбранным для своего ингредиента. Ответ — Id продукта.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new CreateProductCommand(CurrentUserId, request.ToFields()), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateProductCommand(id, CurrentUserId, request.ToFields()), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteProductCommand(id, CurrentUserId), cancellationToken));

    /// <summary>Выбрать продукт: рецепты с его ингредиентом пересчитаются по нему у всей семьи.</summary>
    [HttpPost("{id:guid}/select")]
    public async Task<IActionResult> Select(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new SelectProductCommand(id, CurrentUserId), cancellationToken));
}
