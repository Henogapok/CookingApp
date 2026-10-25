namespace Cooking.Api.Contracts;

public record CreateRecipeDraftRequest(string Text);

public record CorrectRecipeDraftRequest(string Text);

public record CreateRecipeDraftFromUrlRequest(string Url);

public record SelectRecipeDraftDishesRequest(List<int> DishIndexes);
