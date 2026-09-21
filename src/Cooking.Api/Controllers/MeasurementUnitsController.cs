using Cooking.Api.Contracts;
using Cooking.Application.MeasurementUnits.Commands;
using Cooking.Application.MeasurementUnits.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class MeasurementUnitsController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetMeasurementUnitsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new GetMeasurementUnitByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(MeasurementUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateMeasurementUnitCommand(request.Name, request.Abbreviation), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MeasurementUnitRequest request, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new UpdateMeasurementUnitCommand(id, request.Name, request.Abbreviation), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await Mediator.Send(new DeleteMeasurementUnitCommand(id), cancellationToken));
}
