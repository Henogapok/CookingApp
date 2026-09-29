namespace Cooking.Application.RecipeDrafts.Parsing;

/// <summary>
/// Ответ LLM: в одном тексте может быть несколько блюд (рацион дня, подборка перекусов, варианты начинок).
/// </summary>
/// <param name="Dishes">Названия всех найденных блюд по порядку; пусто — рецепта нет.</param>
/// <param name="Recipes">
/// Полные рецепты. Пусто при непустом Dishes — блюд больше <see cref="RecipeDraftLimits.MaxDishes"/>,
/// и пользователь сначала выбирает, какие разбирать.
/// </param>
public record ParsedRecipes(List<string> Dishes, List<ParsedRecipe> Recipes);

/// <summary>
/// Рецепт, как его вернул LLM: имена и коды (<see cref="RecipeParsingCodes"/>), без наших Id.
/// Перевод в Id и сопоставление с каталогом — <see cref="RecipeDraftMapper"/>.
/// </summary>
/// <param name="Servings">Только если указано в тексте; иначе null, а оценка — в ServingsEstimate.</param>
/// <param name="CookingTimeMinutes">Только если указано в тексте; иначе null, а оценка — в CookingTimeMinutesEstimate.</param>
public record ParsedRecipe(
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
/// PieceWeight — вес 1 шт в базовой единице, если продукт считают штуками.
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
    decimal CarbsPer100G,
    decimal? PieceWeight);

public record ParsedStep(string Instruction, int? TimerSeconds);
