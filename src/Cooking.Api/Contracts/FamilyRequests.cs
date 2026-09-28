namespace Cooking.Api.Contracts;

// UserId в теле — временно, пока нет авторизации через Telegram Login Widget:
// потом он будет браться из токена текущего пользователя.
public record CreateFamilyRequest(Guid UserId, string Name);

public record FamilyActorRequest(Guid UserId);
