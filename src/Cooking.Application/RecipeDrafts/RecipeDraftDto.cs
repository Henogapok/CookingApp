using Cooking.Application.Nutrition;
using Cooking.Application.Recipes;

namespace Cooking.Application.RecipeDrafts;

/// <summary>Черновик для показа пользователю (превью в боте, экран в PWA).</summary>
public record RecipeDraftDto(
    Guid Id,
    string Title,
    string? Description,
    string ComplexityName,
    int? Servings,
    bool ServingsIsEstimate,
    int? CookingTimeMinutes,
    bool CookingTimeIsEstimate,
    bool CanApplyEstimates,
    bool IsBeingCorrected,
    Guid? RecipeId,
    string? SourceUrl,
    List<RecipeDraftIngredientDto> Ingredients,
    List<RecipeStepDto> Steps,
    List<string> TagNames,
    RecipeNutrition Nutrition,
    DateTime ExpiresAt);

/// <param name="IsNew">Ингредиента нет в каталоге — создастся при сохранении.</param>
/// <param name="IsNutritionEstimatedByLlm">КБЖУ — оценка ИИ: у новых всегда, у ингредиентов из каталога — если так записано там.</param>
/// <param name="BaseAmount">Количество в г/мл для расчёта КБЖУ; null — «по вкусу» или не пересчитать.</param>
public record RecipeDraftIngredientDto(
    string Name,
    decimal? Amount,
    Guid? UnitId,
    string? UnitAbbreviation,
    bool IsNew,
    bool IsNutritionEstimatedByLlm,
    decimal? BaseAmount,
    string? BaseUnitAbbreviation,
    NutritionFacts? Nutrition);
