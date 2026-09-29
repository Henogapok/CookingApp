namespace Cooking.Application.RecipeDrafts.Parsing;

/// <summary>
/// Рецепт, как его вернул LLM: имена и коды (<see cref="RecipeParsingCodes"/>), без наших Id.
/// Перевод в Id и сопоставление с каталогом — <see cref="RecipeDraftMapper"/>.
/// </summary>
/// <param name="Servings">Только если указано в тексте; иначе null, а оценка — в ServingsEstimate.</param>
/// <param name="CookingTimeMinutes">Только если указано в тексте; иначе null, а оценка — в CookingTimeMinutesEstimate.</param>
public record ParsedRecipe(
    bool IsRecipe,
    string Title,
    string? Description,
    string Complexity,
    int? Servings,
    int? ServingsEstimate,
    int? CookingTimeMinutes,
    int? CookingTimeMinutesEstimate,
    List<ParsedIngredient> Ingredients,
    List<ParsedStep> Steps,
    List<string> Tags);

/// <summary>
/// Amount/Unit = null — «по вкусу». Category, BaseUnit и КБЖУ LLM заполняет всегда,
/// но используются они только для ингредиентов, которых нет в каталоге.
/// </summary>
public record ParsedIngredient(
    string Name,
    decimal? Amount,
    string? Unit,
    string Category,
    string BaseUnit,
    decimal CaloriesPer100G,
    decimal ProteinPer100G,
    decimal FatPer100G,
    decimal CarbsPer100G);

public record ParsedStep(string Instruction, int? TimerSeconds);
