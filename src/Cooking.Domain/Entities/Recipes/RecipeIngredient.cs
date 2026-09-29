using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Ingredients;

namespace Cooking.Domain.Entities.Recipes;

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid IngredientCatalogId { get; set; }
    public IngredientCatalog IngredientCatalog { get; set; } = null!;

    /// <summary>null вместе с UnitId — «по вкусу»: такой ингредиент не идёт в расчёт КБЖУ/стоимости.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Единица в этом рецепте — может отличаться от базовой единицы ингредиента.</summary>
    public Guid? UnitId { get; set; }
    public MeasurementUnit? Unit { get; set; }

    /// <summary>Порядок отображения в карточке рецепта.</summary>
    public int SortOrder { get; set; }
}
