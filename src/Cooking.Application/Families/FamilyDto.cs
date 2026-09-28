namespace Cooking.Application.Families;

public record FamilyDto(Guid Id, string Name, List<FamilyMemberDto> Members);

public record FamilyMemberDto(Guid UserId, string FirstName, string? LastName);
