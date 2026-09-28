using Cooking.Api.Contracts;
using Cooking.Application.Families.Commands;
using Cooking.Application.Families.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class FamiliesController : BaseController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetFamilyByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateFamilyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateFamilyCommand(request.UserId, request.Name), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPost("{id:guid}/invites")]
    public async Task<IActionResult> CreateInvite(Guid id, FamilyActorRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new CreateFamilyInviteCommand(id, request.UserId), cancellationToken));

    [HttpPost("invites/{code}/accept")]
    public async Task<IActionResult> AcceptInvite(string code, FamilyActorRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new AcceptFamilyInviteCommand(code, request.UserId), cancellationToken));

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, FamilyActorRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new LeaveFamilyCommand(id, request.UserId), cancellationToken));
}
