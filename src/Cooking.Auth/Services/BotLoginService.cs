using Cooking.Application.Auth;
using Cooking.Application.Common.Errors;
using Cooking.Application.Users;
using Cooking.Auth.Identity;
using FluentResults;

namespace Cooking.Auth.Services;

/// <summary>
/// Вход через бота со стороны сайта: начать (ссылка на бота + секрет браузера) и опрашивать, подтвердил ли
/// пользователь вход. Сами запросы на вход — в Application (LoginRequest), здесь только перевод в понятия сайта.
/// PollToken — секрет браузера: в лог не пишем, только Code (он и так открыт — в ссылке на бота).
/// </summary>
public class BotLoginService(
    ILoginRequestRepositoryService loginRequests,
    IUserRepositoryService users,
    TelegramBotInfo botInfo,
    ILogger<BotLoginService> logger)
{
    /// <summary>Длиннее PollToken не бывает — такую cookie не ищем в базе.</summary>
    private const int MaxPollTokenLength = 128;

    public async Task<Result<BotLoginStarted>> StartAsync(CancellationToken cancellationToken)
    {
        if (await botInfo.GetUsernameAsync(cancellationToken) is not { } botUsername)
        {
            logger.LogWarning("Bot login start failed: bot is not configured (no Telegram:BotUsername / BotToken)");
            return Result.Fail(new AppError("Bot login is not configured.", ErrorCode.Unavailable));
        }

        var created = await loginRequests.CreateAsync(cancellationToken);
        if (created.IsFailed)
        {
            logger.LogWarning("Bot login start failed: {Errors}", created.Errors);
            return created.ToResult<BotLoginStarted>();
        }

        var request = created.Value;
        logger.LogInformation("Bot login started: code {Code}, expires at {ExpiresAt}", request.Code, request.ExpiresAt);
        return Result.Ok(new BotLoginStarted(LoginRequestLink.Build(botUsername, request.Code), request.PollToken, request.ExpiresAt));
    }

    public async Task<Result<BotLoginPoll>> PollAsync(string? pollToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(pollToken) || pollToken.Length > MaxPollTokenLength)
        {
            logger.LogDebug("Bot login poll: no or invalid poll cookie → expired");
            return Result.Ok(new BotLoginPoll(BotLoginStatus.Expired));
        }

        var completed = await loginRequests.CompleteAsync(pollToken, cancellationToken);
        if (completed.IsFailed)
        {
            // Не найден / истёк / уже использован — для браузера одно и то же: начать заново.
            if (completed.Errors.OfType<AppError>().Any(e => e.Code is ErrorCode.NotFound or ErrorCode.Validation))
            {
                logger.LogInformation("Bot login poll: request not found or expired");
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Expired));
            }

            logger.LogWarning("Bot login poll failed: {Errors}", completed.Errors);
            return completed.ToResult<BotLoginPoll>();
        }

        switch (completed.Value)
        {
            case { State: LoginRequestState.Confirmed, UserId: { } userId }:
                var user = await users.GetByIdAsync(userId, cancellationToken);
                if (user.IsFailed)
                {
                    logger.LogWarning("Bot login confirmed by user {UserId}, but the user can't be loaded: {Errors}", userId, user.Errors);
                    return user.ToResult<BotLoginPoll>();
                }

                logger.LogInformation("Bot login confirmed: user {UserId} ({UserName}) signed in", userId, user.Value.FirstName);
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Confirmed, UserPrincipalFactory.CreateSession(user.Value)));

            case { State: LoginRequestState.Declined }:
                logger.LogInformation("Bot login declined in the bot");
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Declined));

            default:
                // Браузер опрашивает раз в пару секунд — на Information забило бы лог.
                logger.LogDebug("Bot login poll: still pending");
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Pending));
        }
    }
}
