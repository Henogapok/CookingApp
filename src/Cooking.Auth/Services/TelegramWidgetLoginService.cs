using System.Security.Claims;
using Cooking.Application.Common.Errors;
using Cooking.Application.Users;
using Cooking.Auth.Identity;
using Cooking.Auth.Options;
using FluentResults;
using Microsoft.Extensions.Options;

namespace Cooking.Auth.Services;

/// <summary>
/// Вход через Telegram Login Widget: проверить подпись данных от Telegram и найти (или зарегистрировать) пользователя.
/// Регистрация — та же идемпотентная, что на /start в боте: войти можно, даже ни разу не написав боту.
/// Данные виджета в лог не пишем — только итог и причину отказа.
/// </summary>
public class TelegramWidgetLoginService(
    IUserRepositoryService users,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWidgetLoginService> logger)
{
    public bool IsEnabled => options.Value is { LoginWidgetEnabled: true, BotToken.Length: > 0 };

    /// <summary>Готовая сессия (её осталось открыть) или Validation, если данные не от Telegram/устарели.</summary>
    public async Task<Result<ClaimsPrincipal>> SignInAsync(IReadOnlyDictionary<string, string> fields, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return Result.Fail(new AppError("Telegram Login Widget is not configured.", ErrorCode.Unavailable));

        var verified = TelegramWidgetSignature.Verify(fields, options.Value.BotToken!, DateTimeOffset.UtcNow);
        if (verified.IsFailed)
        {
            logger.LogWarning("Telegram widget login rejected: {Reason}", verified.Errors[0].Message);
            return Result.Fail(new AppError("Invalid Telegram login data.", ErrorCode.Validation));
        }

        var telegramUser = verified.Value;
        var user = await users.RegisterAsync(telegramUser.TelegramId, telegramUser.FirstName, telegramUser.LastName, cancellationToken);
        if (user.IsFailed)
        {
            logger.LogWarning("Telegram widget login: user can't be registered: {Errors}", user.Errors);
            return user.ToResult<ClaimsPrincipal>();
        }

        logger.LogInformation("Telegram widget login: user {UserId} ({UserName}) signed in", user.Value.Id, user.Value.FirstName);
        return Result.Ok(UserPrincipalFactory.CreateSession(user.Value));
    }
}
