using Cooking.Api.Contracts;
using Cooking.Application.Tags.Commands;
using Cooking.Application.Tags.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class TagsController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tagTypeId, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetTagsQuery(tagTypeId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetTagByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(TagRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateTagCommand(request.Name, request.TagTypeId), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TagRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateTagCommand(id, request.Name, request.TagTypeId), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteTagCommand(id), cancellationToken));
}
