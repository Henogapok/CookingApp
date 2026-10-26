using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Users;

namespace Cooking.Domain.Entities.Ingredients;

/// <summary>
/// То, что пользователь реально покупает («Lactel 3,2%, 520 ₸ за 1 л»), для общего ингредиента из каталога («Молоко 3,2%»).
/// Рецепты ссылаются на ингредиент, а цену и КБЖУ при показе берут из выбранного продукта того, кто смотрит.
/// Видят и правят продукт автор и участники его семьи (как рецепты): у семьи общий холодильник.
/// </summary>
public class Product : BaseEntity
{
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid IngredientCatalogId { get; set; }
    public IngredientCatalog IngredientCatalog { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>
    /// Цена и размер упаковки — ровно как ввели с ценника («520 ₸ · 1 л»). Цену за 100 базовых единиц не храним,
    /// а считаем: иначе она разойдётся с упаковкой. Все три null — цена не указана (продукт только ради КБЖУ).
    /// </summary>
    public decimal? PackagePrice { get; set; }
    public decimal? PackageAmount { get; set; }
    public Guid? PackageUnitId { get; set; }
    public MeasurementUnit? PackageUnit { get; set; }

    /// <summary>КБЖУ с упаковки на 100 базовых единиц ингредиента; все null — берём КБЖУ из каталога.</summary>
    public decimal? CaloriesPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }

    /// <summary>
    /// Когда продукт последний раз выбрали (или создали). Для ингредиента в расчёт идёт видимый продукт с самым поздним
    /// временем — так выбор одного участника сразу действует на всю семью, а «выбранного» флага, который надо снимать
    /// с остальных, нет.
    /// </summary>
    public DateTime SelectedAt { get; set; }
}
