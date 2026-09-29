using Cooking.Application.Recipes;

namespace Cooking.Api.Contracts;

// UserId в теле — временно, пока нет авторизации через Telegram Login Widget:
// потом он будет браться из токена текущего пользователя.
public record RecipeRequest(
    Guid UserId,
    string Title,
    string? Description,
    string? SourceUrl,
    Guid SourceTypeId,
    Guid ComplexityId,
    int? Servings,
    int? CookingTimeMinutes,
    List<RecipeIngredientRequest> Ingredients,
    List<RecipeStepRequest> Steps,
    List<Guid> TagIds)
{
    // Списки, не переданные в JSON, приходят null — считаем их пустыми.
    public RecipeFields ToFields() => new(
        Title,
        Description,
        SourceUrl,
        SourceTypeId,
        ComplexityId,
        Servings,
        CookingTimeMinutes,
        (Ingredients ?? []).Select(i => new RecipeIngredientFields(i.IngredientCatalogId, i.Amount, i.UnitId)).ToList(),
        (Steps ?? []).Select(s => new RecipeStepFields(s.Instruction, s.TimerSeconds)).ToList(),
        TagIds ?? []);
}

/// <summary>Порядок ингредиентов в карточке = порядок в списке. Amount и UnitId = null — «по вкусу».</summary>
public record RecipeIngredientRequest(Guid IngredientCatalogId, decimal? Amount, Guid? UnitId);

/// <summary>Номер шага = позиция в списке (с 1).</summary>
public record RecipeStepRequest(string Instruction, int? TimerSeconds);
