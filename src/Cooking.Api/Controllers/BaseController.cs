using Cooking.Application.Common.Errors;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cooking.Api.Controllers;

// Всё API — только с токеном от Cooking.Auth; открытые эндпоинты помечены [AllowAnonymous].
[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Текущий пользователь — claim sub из access token'а (Id пользователя в нашей базе).</summary>
    protected Guid CurrentUserId => Guid.TryParse(User.FindFirst("sub")?.Value, out var id)
        ? id
        : throw new InvalidOperationException("Access token has no valid 'sub' claim.");

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : HandleFailure(result);

    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : HandleFailure(result);

    private IActionResult HandleFailure(ResultBase result)
    {
        var code = result.Errors.OfType<AppError>().FirstOrDefault()?.Code;

        var (title, statusCode) = code switch
        {
            ErrorCode.NotFound => ("Не удалось найти информацию", StatusCodes.Status404NotFound),
            ErrorCode.Validation => ("Невалидный параметр", StatusCodes.Status400BadRequest),
            ErrorCode.LogicConflict => ("Конфликт логической зависимости", StatusCodes.Status409Conflict),
            ErrorCode.Forbidden => ("Недостаточно прав", StatusCodes.Status403Forbidden),
            ErrorCode.Unavailable => ("Функция недоступна", StatusCodes.Status503ServiceUnavailable),
            ErrorCode.ExternalService => ("Ошибка внешнего сервиса", StatusCodes.Status502BadGateway),
            _ => ("Необработанное исключение", StatusCodes.Status500InternalServerError)
        };

        var problemDetails = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: statusCode,
            title: title,
            detail: string.Join(" ", result.Errors.Select(e => e.Message)));

        problemDetails.Extensions["errors"] = result.Errors.Select(e => e.Message).ToArray();

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
