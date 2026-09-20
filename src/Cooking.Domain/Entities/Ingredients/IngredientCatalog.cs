using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Recipes;

namespace Cooking.Domain.Entities.Ingredients;

public class IngredientCatalog : BaseEntity
{
    public required string Name { get; set; }

    public Guid CategoryId { get; set; }
    public IngredientCategory Category { get; set; } = null!;

    /// <summary>Базовая единица для расчёта КБЖУ (все Per100g-поля привязаны к ней).</summary>
    public Guid BaseUnitId { get; set; }
    public MeasurementUnit BaseUnit { get; set; } = null!;

    public decimal PricePer100g { get; set; }
    public decimal CaloriesPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal FatPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }

    /// <summary>Кто создал запись (Manual, LLM, FatSecret).</summary>
    public Guid CreatedBySourceId { get; set; }
    public DataSource CreatedBySource { get; set; } = null!;

    /// <summary>Откуда взято КБЖУ (Manual, LLM, FatSecret).</summary>
    public Guid NutritionSourceId { get; set; }
    public DataSource NutritionSource { get; set; } = null!;

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}
