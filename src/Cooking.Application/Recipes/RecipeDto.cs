using Cooking.Application.Tags;

using Cooking.Application.Nutrition;

namespace Cooking.Application.Recipes;

public record RecipeDto(
    Guid Id,
    string Title,
    string? Description,
    string? SourceUrl,
    Guid SourceTypeId,
    string SourceTypeName,
    Guid ComplexityId,
    string ComplexityName,
    int? Servings,
    int? CookingTimeMinutes,
    Guid CreatedByUserId,
    string CreatedByFirstName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<RecipeIngredientDto> Ingredients,
    List<RecipeStepDto> Steps,
    List<TagDto> Tags,
    RecipeNutrition Nutrition);

public record RecipeIngredientDto(
    Guid IngredientCatalogId,
    string IngredientName,
    decimal? Amount,
    Guid? UnitId,
    string? UnitName,
    string? UnitAbbreviation,
    bool IsNutritionEstimatedByLlm,
    decimal? BaseAmount,
    string BaseUnitAbbreviation,
    NutritionFacts? Nutrition);

public record RecipeStepDto(int StepNumber, string Instruction, int? TimerSeconds);

/// <summary>
/// Краткая карточка для списков и поиска. PWA загружает список целиком и сама ищет и группирует
/// (по тегам, дате), поэтому здесь всё, что нужно карточке и полкам, без ингредиентов и шагов.
/// </summary>
/// <param name="NutritionPerServing">null, если число порций неизвестно.</param>
public record RecipeSummaryDto(
    Guid Id,
    string Title,
    string ComplexityName,
    int? Servings,
    int? CookingTimeMinutes,
    Guid CreatedByUserId,
    string CreatedByFirstName,
    DateTime CreatedAt,
    List<TagDto> Tags,
    NutritionFacts? NutritionPerServing);
