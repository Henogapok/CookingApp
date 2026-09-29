using Cooking.Application.Recipes;

namespace Cooking.Application.RecipeDrafts;

/// <summary>
/// Разобранный рецепт в черновике (хранится JSON'ом). Всё уже переведено в наши Id;
/// ингредиент — либо ссылка на каталог, либо данные нового ингредиента, который создастся при сохранении.
/// </summary>
/// <param name="UseEstimates">Пользователь попросил подставить оценки ИИ вместо не указанных порций/времени.</param>
public record RecipeDraftContent(
    string Title,
    string? Description,
    Guid ComplexityId,
    int? Servings,
    int? ServingsEstimate,
    int? CookingTimeMinutes,
    int? CookingTimeMinutesEstimate,
    bool UseEstimates,
    List<RecipeDraftIngredient> Ingredients,
    List<RecipeStepFields> Steps,
    List<Guid> TagIds)
{
    public int? EffectiveServings => Servings ?? (UseEstimates ? ServingsEstimate : null);

    public int? EffectiveCookingTimeMinutes => CookingTimeMinutes ?? (UseEstimates ? CookingTimeMinutesEstimate : null);

    /// <summary>Есть что оценить: значение не указано в тексте, а у ИИ есть оценка.</summary>
    public bool CanApplyEstimates =>
        !UseEstimates
        && ((Servings is null && ServingsEstimate is not null)
            || (CookingTimeMinutes is null && CookingTimeMinutesEstimate is not null));
}

/// <summary>Ровно одно из IngredientCatalogId / NewIngredient заполнено. Amount/UnitId = null — «по вкусу».</summary>
public record RecipeDraftIngredient(
    string Name,
    Guid? IngredientCatalogId,
    NewIngredientDraft? NewIngredient,
    decimal? Amount,
    Guid? UnitId);

/// <summary>КБЖУ оценил LLM; цена неизвестна (0) — её вносят вручную.</summary>
public record NewIngredientDraft(
    Guid CategoryId,
    Guid BaseUnitId,
    decimal CaloriesPer100G,
    decimal ProteinPer100G,
    decimal FatPer100G,
    decimal CarbsPer100G);
