using Cooking.Application.HealthChecks.Queries.CheckDatabaseConnection;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

public class HealthCheckController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var isConnected = await Mediator.Send(new CheckDatabaseConnectionQuery(), cancellationToken);

        return isConnected
            ? Ok(new { status = "Healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
