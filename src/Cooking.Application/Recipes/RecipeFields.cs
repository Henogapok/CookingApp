namespace Cooking.Application.Recipes;

/// <summary>
/// Полное содержимое рецепта для создания/обновления. Порядок ингредиентов и номера шагов
/// берутся из позиции в списке, поэтому клиенту не нужно их нумеровать.
/// </summary>
public record RecipeFields(
    string Title,
    string? Description,
    string? SourceUrl,
    Guid SourceTypeId,
    Guid ComplexityId,
    int Servings,
    int CookingTimeMinutes,
    List<RecipeIngredientFields> Ingredients,
    List<RecipeStepFields> Steps,
    List<Guid> TagIds);

public record RecipeIngredientFields(Guid IngredientCatalogId, decimal Amount, Guid UnitId);

public record RecipeStepFields(string Instruction, int? TimerSeconds);
