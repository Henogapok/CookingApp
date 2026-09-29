namespace Cooking.Api.Contracts;

// UserId в теле — временно, пока нет авторизации через Telegram Login Widget.
public record CreateRecipeDraftRequest(Guid UserId, string Text);

public record RecipeDraftActorRequest(Guid UserId);

public record CorrectRecipeDraftRequest(Guid UserId, string Text);
