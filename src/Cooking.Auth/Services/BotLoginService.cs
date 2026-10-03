using Cooking.Application.Auth;
using Cooking.Application.Auth.Commands;
using Cooking.Application.Common.Errors;
using Cooking.Application.Users.Queries;
using Cooking.Auth.Identity;
using FluentResults;
using MediatR;

namespace Cooking.Auth.Services;

/// <summary>
/// Вход через бота со стороны сайта: начать (ссылка на бота + секрет браузера) и опрашивать, подтвердил ли
/// пользователь вход. Сами запросы на вход — в Application (LoginRequest), здесь только перевод в понятия сайта.
/// </summary>
public class BotLoginService(ISender mediator, TelegramBotInfo botInfo)
{
    public async Task<Result<BotLoginStarted>> StartAsync(CancellationToken cancellationToken)
    {
        if (await botInfo.GetUsernameAsync(cancellationToken) is not { } botUsername)
            return Result.Fail(new AppError("Bot login is not configured.", ErrorCode.Unavailable));

        var created = await mediator.Send(new CreateLoginRequestCommand(), cancellationToken);
        if (created.IsFailed)
            return created.ToResult<BotLoginStarted>();

        var request = created.Value;
        return Result.Ok(new BotLoginStarted(LoginRequestLink.Build(botUsername, request.Code), request.PollToken, request.ExpiresAt));
    }

    public async Task<Result<BotLoginPoll>> PollAsync(string? pollToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(pollToken))
            return Result.Ok(new BotLoginPoll(BotLoginStatus.Expired));

        var completed = await mediator.Send(new CompleteLoginRequestCommand(pollToken), cancellationToken);
        if (completed.IsFailed)
        {
            // Не найден / истёк / испорченная cookie — для браузера одно и то же: начать заново.
            return completed.Errors.OfType<AppError>().Any(e => e.Code is ErrorCode.NotFound or ErrorCode.Validation)
                ? Result.Ok(new BotLoginPoll(BotLoginStatus.Expired))
                : completed.ToResult<BotLoginPoll>();
        }

        switch (completed.Value)
        {
            case { State: LoginRequestState.Confirmed, UserId: { } userId }:
                var user = await mediator.Send(new GetUserByIdQuery(userId), cancellationToken);
                return user.IsSuccess
                    ? Result.Ok(new BotLoginPoll(BotLoginStatus.Confirmed, UserPrincipalFactory.CreateSession(user.Value)))
                    : user.ToResult<BotLoginPoll>();

            case { State: LoginRequestState.Declined }:
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Declined));

            default:
                return Result.Ok(new BotLoginPoll(BotLoginStatus.Pending));
        }
    }
}
