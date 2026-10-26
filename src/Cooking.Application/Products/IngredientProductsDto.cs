using Cooking.Application.Nutrition;

namespace Cooking.Application.Products;

/// <summary>
/// Ингредиент на экране «Мои продукты»: из рецептов семьи или с продуктами семьи. Продуктов может не быть —
/// тогда рецепты считаются по КБЖУ каталога и без цены.
/// </summary>
/// <param name="CatalogPer100">КБЖУ из общего каталога — по умолчанию, если у продукта нет своего.</param>
/// <param name="RecipeCount">В скольких рецептах семьи встречается.</param>
public record IngredientProductsDto(
    Guid IngredientCatalogId,
    string Name,
    Guid BaseUnitId,
    string BaseUnitAbbreviation,
    decimal? PieceWeight,
    NutritionFacts CatalogPer100,
    bool IsNutritionEstimatedByLlm,
    int RecipeCount,
    List<ProductDto> Products);
