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
    List<RecipeDraftIngredientDto> Ingredients,
    List<RecipeStepDto> Steps,
    List<string> TagNames,
    DateTime ExpiresAt);

/// <param name="IsNew">Ингредиента нет в каталоге — создастся при сохранении, КБЖУ оценил ИИ.</param>
public record RecipeDraftIngredientDto(string Name, decimal? Amount, string? UnitAbbreviation, bool IsNew);
