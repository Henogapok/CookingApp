using System.Security.Claims;

namespace Cooking.Auth.Services;

/// <summary>BotLink — показать на странице, PollToken — в HttpOnly cookie браузера.</summary>
public record BotLoginStarted(string BotLink, string PollToken, DateTime ExpiresAt);

public enum BotLoginStatus
{
    /// <summary>Ждём нажатия «Войти» в боте.</summary>
    Pending,

    /// <summary>Подтверждено — в Session готовая сессия, её осталось открыть.</summary>
    Confirmed,

    Declined,

    /// <summary>Истёк, уже использован или не найден — начать заново.</summary>
    Expired,
}

public record BotLoginPoll(BotLoginStatus Status, ClaimsPrincipal? Session = null);
