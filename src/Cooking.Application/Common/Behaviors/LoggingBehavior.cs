using System.Diagnostics;
using System.Reflection;
using Cooking.Application.Common.Errors;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Cooking.Application.Common.Behaviors;

/// <summary>
/// Одна строка в лог на каждый запрос: что, от кого, за сколько и чем закончилось. Бот и контроллеры ходят
/// через MediatR, так что это журнал действий пользователей. Команды — Information; запросы (*Query) — Debug:
/// бот читает пользователя на каждый апдейт, на Information они бы забили лог.
/// Содержимое запроса не пишем — там тексты рецептов и правок.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase
{
    private static readonly string RequestName = typeof(TRequest).Name;

    private static readonly LogLevel Level = RequestName.EndsWith("Query", StringComparison.Ordinal)
        ? LogLevel.Debug
        : LogLevel.Information;

    // Почти у всех команд есть UserId — кто действовал; ищем один раз на тип запроса.
    private static readonly PropertyInfo? UserIdProperty = typeof(TRequest).GetProperty("UserId");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!logger.IsEnabled(Level))
            return await next();

        var started = Stopwatch.GetTimestamp();
        var response = await next();
        var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var outcome = response.IsSuccess
            ? "succeeded"
            : "failed: " + string.Join("; ", response.Errors.Select(e => e is AppError app ? $"{app.Code}: {e.Message}" : e.Message));

        // Фоновые команды (разбор черновика) без UserId — пишем без него, а не "by user null".
        if (UserIdProperty?.GetValue(request) is { } userId)
            logger.Log(Level, "{Request} by user {UserId} in {ElapsedMs} ms {Outcome}", RequestName, userId, elapsedMs, outcome);
        else
            logger.Log(Level, "{Request} in {ElapsedMs} ms {Outcome}", RequestName, elapsedMs, outcome);

        return response;
    }
}
