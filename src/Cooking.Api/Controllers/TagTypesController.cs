using Cooking.Api.Contracts;
using Cooking.Application.ReferenceData.Commands;
using Cooking.Application.ReferenceData.Queries;
using Cooking.Domain.Entities.Tags;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class TagTypesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetReferenceEntitiesQuery<TagType>(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetReferenceEntityByIdQuery<TagType>(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(ReferenceEntityRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateReferenceEntityCommand<TagType>(request.Name), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ReferenceEntityRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateReferenceEntityCommand<TagType>(id, request.Name), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteReferenceEntityCommand<TagType>(id), cancellationToken));
}
