using Cooking.Application.Common.Errors;
using FluentResults;
using FluentValidation;
using MediatR;

namespace Cooking.Application.Common.Behaviors;

/// <summary>
/// Прогоняет все зарегистрированные FluentValidation-валидаторы для запроса перед хендлером.
/// TResponse ограничен ResultBase, new() (паттерн из официального примера FluentResults для MediatR:
/// altmann/FluentResults/src/FluentResults.Samples.MediatR) — так можно сконструировать провальный
/// Result/Result&lt;T&gt; для любого закрытого generic-варианта без reflection.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var result = new TResponse();
        foreach (var failure in failures)
            result.Reasons.Add(new AppError(failure.ErrorMessage, ErrorCode.Validation));

        return result;
    }
}
