using Cooking.Application.Common.Errors;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : HandleFailure(result);

    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : HandleFailure(result);

    private IActionResult HandleFailure(ResultBase result) =>
        result.Errors.Any(e => e is NotFoundError)
            ? NotFound(new { errors = result.Errors.Select(e => e.Message) })
            : BadRequest(new { errors = result.Errors.Select(e => e.Message) });
}
