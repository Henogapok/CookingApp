namespace Cooking.Application.Users;

public record UserDto(
    Guid Id,
    long TelegramId,
    string FirstName,
    string? LastName,
    Guid? FamilyId,
    string? FamilyName);
