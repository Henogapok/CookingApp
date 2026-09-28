namespace Cooking.Application.Families;

public record FamilyInviteDto(string Code, Guid FamilyId, DateTime ExpiresAt);
