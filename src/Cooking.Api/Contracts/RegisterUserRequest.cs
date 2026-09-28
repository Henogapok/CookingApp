namespace Cooking.Api.Contracts;

public record RegisterUserRequest(long TelegramId, string FirstName, string? LastName);
