using Cooking.Application.Tags;

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
    int Servings,
    int CookingTimeMinutes,
    Guid CreatedByUserId,
    string CreatedByFirstName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<RecipeIngredientDto> Ingredients,
    List<RecipeStepDto> Steps,
    List<TagDto> Tags);

public record RecipeIngredientDto(
    Guid IngredientCatalogId,
    string IngredientName,
    decimal Amount,
    Guid UnitId,
    string UnitName,
    string UnitAbbreviation);

public record RecipeStepDto(int StepNumber, string Instruction, int? TimerSeconds);

/// <summary>Краткая карточка для списков и поиска.</summary>
public record RecipeSummaryDto(
    Guid Id,
    string Title,
    string ComplexityName,
    int Servings,
    int CookingTimeMinutes,
    Guid CreatedByUserId,
    string CreatedByFirstName);
