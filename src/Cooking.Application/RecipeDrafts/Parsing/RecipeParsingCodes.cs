using Cooking.Domain.ReferenceData;

namespace Cooking.Application.RecipeDrafts.Parsing;

/// <summary>
/// Коды, которыми LLM обозначает справочные значения (они же — enum'ы в JSON-схеме ответа),
/// и соответствующие им Id из seed'а.
/// </summary>
public static class RecipeParsingCodes
{
    public static readonly IReadOnlyDictionary<string, Guid> Complexities = new Dictionary<string, Guid>
    {
        ["easy"] = ReferenceIds.Complexities.Easy,
        ["medium"] = ReferenceIds.Complexities.Medium,
        ["hard"] = ReferenceIds.Complexities.Hard,
    };

    public static readonly IReadOnlyDictionary<string, Guid> Units = new Dictionary<string, Guid>
    {
        ["g"] = ReferenceIds.MeasurementUnits.Gram,
        ["ml"] = ReferenceIds.MeasurementUnits.Milliliter,
        ["pcs"] = ReferenceIds.MeasurementUnits.Piece,
        ["tbsp"] = ReferenceIds.MeasurementUnits.Tablespoon,
        ["tsp"] = ReferenceIds.MeasurementUnits.Teaspoon,
        ["cup"] = ReferenceIds.MeasurementUnits.Glass,
        ["pinch"] = ReferenceIds.MeasurementUnits.Pinch,
    };

    /// <summary>КБЖУ в каталоге — на 100 г или 100 мл, поэтому базовая единица только из этих двух.</summary>
    public static readonly IReadOnlyDictionary<string, Guid> BaseUnits = new Dictionary<string, Guid>
    {
        ["g"] = ReferenceIds.MeasurementUnits.Gram,
        ["ml"] = ReferenceIds.MeasurementUnits.Milliliter,
    };

    public static readonly IReadOnlyDictionary<string, Guid> Categories = new Dictionary<string, Guid>
    {
        ["meat"] = ReferenceIds.IngredientCategories.Meat,
        ["poultry"] = ReferenceIds.IngredientCategories.Poultry,
        ["seafood"] = ReferenceIds.IngredientCategories.Seafood,
        ["vegetables"] = ReferenceIds.IngredientCategories.Vegetables,
        ["fruits"] = ReferenceIds.IngredientCategories.Fruits,
        ["grains_legumes"] = ReferenceIds.IngredientCategories.GrainsAndLegumes,
        ["dairy"] = ReferenceIds.IngredientCategories.Dairy,
        ["eggs"] = ReferenceIds.IngredientCategories.Eggs,
        ["oils_fats"] = ReferenceIds.IngredientCategories.OilsAndFats,
        ["flour_bakery"] = ReferenceIds.IngredientCategories.FlourAndBakery,
        ["spices"] = ReferenceIds.IngredientCategories.Spices,
        ["drinks"] = ReferenceIds.IngredientCategories.Drinks,
    };
}
