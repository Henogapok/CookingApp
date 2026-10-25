namespace Cooking.Application.Auth;

/// <summary>Code — в ссылку на бота, PollToken — в cookie браузера (секрет, в базе только хеш).</summary>
public record LoginRequestCreatedDto(string Code, string PollToken, DateTime ExpiresAt);

public enum LoginRequestState
{
    Pending,
    Confirmed,
    Declined,
}

/// <summary>UserId заполнен только при Confirmed.</summary>
public record LoginRequestStatusDto(LoginRequestState State, Guid? UserId);
