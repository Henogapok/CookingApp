using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cooking.Api.ExceptionHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string PostgresUniqueViolationSqlState = "23505";
    private const string PostgresForeignKeyViolationSqlState = "23503";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresUniqueViolationSqlState } } =>
                (StatusCodes.Status409Conflict, "Такая запись уже есть"),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresForeignKeyViolationSqlState } } =>
                (StatusCodes.Status409Conflict, "Запись используется другими данными, удалить нельзя"),
            _ => (StatusCodes.Status500InternalServerError, "Что-то пошло не так. Попробуйте ещё раз")
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title
        }, cancellationToken);

        return true;
    }
}
