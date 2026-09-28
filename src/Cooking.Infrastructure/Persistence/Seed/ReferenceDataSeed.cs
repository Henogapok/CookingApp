using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.ReferenceData;

namespace Cooking.Infrastructure.Persistence.Seed;

/// <summary>
/// Начальные значения справочников. Попадают в БД через HasData → миграции, поэтому новая база
/// (например, на проде) сразу пригодна к работе. Id фиксированные — см. <see cref="ReferenceIds"/>.
/// Добавил/поменял значение здесь → нужна новая миграция.
/// </summary>
internal static class ReferenceDataSeed
{
    public static readonly SourceType[] SourceTypes =
    [
        new() { Id = ReferenceIds.SourceTypes.Manual, Name = "Manual" },
        new() { Id = ReferenceIds.SourceTypes.Instagram, Name = "Instagram" },
        new() { Id = ReferenceIds.SourceTypes.YouTube, Name = "YouTube" },
        new() { Id = ReferenceIds.SourceTypes.Website, Name = "Website" },
    ];

    public static readonly Complexity[] Complexities =
    [
        new() { Id = ReferenceIds.Complexities.Easy, Name = "Easy" },
        new() { Id = ReferenceIds.Complexities.Medium, Name = "Medium" },
        new() { Id = ReferenceIds.Complexities.Hard, Name = "Hard" },
    ];

    public static readonly MeasurementUnit[] MeasurementUnits =
    [
        new() { Id = ReferenceIds.MeasurementUnits.Gram, Name = "Грамм", Abbreviation = "г" },
        new() { Id = ReferenceIds.MeasurementUnits.Milliliter, Name = "Миллилитр", Abbreviation = "мл" },
        new() { Id = ReferenceIds.MeasurementUnits.Piece, Name = "Штука", Abbreviation = "шт" },
        new() { Id = ReferenceIds.MeasurementUnits.Tablespoon, Name = "Столовая ложка", Abbreviation = "ст.л." },
        new() { Id = ReferenceIds.MeasurementUnits.Teaspoon, Name = "Чайная ложка", Abbreviation = "ч.л." },
        new() { Id = ReferenceIds.MeasurementUnits.Glass, Name = "Стакан", Abbreviation = "стак." },
        new() { Id = ReferenceIds.MeasurementUnits.Pinch, Name = "Щепотка", Abbreviation = "щеп." },
    ];

    public static readonly DataSource[] DataSources =
    [
        new() { Id = ReferenceIds.DataSources.Manual, Name = "Manual" },
        new() { Id = ReferenceIds.DataSources.Llm, Name = "LLM" },
        new() { Id = ReferenceIds.DataSources.FatSecret, Name = "FatSecret" },
    ];

    public static readonly IngredientCategory[] IngredientCategories =
    [
        new() { Id = ReferenceIds.IngredientCategories.Meat, Name = "Мясо" },
        new() { Id = ReferenceIds.IngredientCategories.Poultry, Name = "Птица" },
        new() { Id = ReferenceIds.IngredientCategories.Seafood, Name = "Рыба и морепродукты" },
        new() { Id = ReferenceIds.IngredientCategories.Vegetables, Name = "Овощи" },
        new() { Id = ReferenceIds.IngredientCategories.Fruits, Name = "Фрукты" },
        new() { Id = ReferenceIds.IngredientCategories.GrainsAndLegumes, Name = "Крупы и бобовые" },
        new() { Id = ReferenceIds.IngredientCategories.Dairy, Name = "Молочные продукты" },
        new() { Id = ReferenceIds.IngredientCategories.Eggs, Name = "Яйца" },
        new() { Id = ReferenceIds.IngredientCategories.OilsAndFats, Name = "Масла и жиры" },
        new() { Id = ReferenceIds.IngredientCategories.FlourAndBakery, Name = "Мучное и выпечка" },
        new() { Id = ReferenceIds.IngredientCategories.Spices, Name = "Специи и приправы" },
        new() { Id = ReferenceIds.IngredientCategories.Drinks, Name = "Напитки" },
    ];

    public static readonly TagType[] TagTypes =
    [
        new() { Id = ReferenceIds.TagTypes.MealType, Name = "MealType" },
        new() { Id = ReferenceIds.TagTypes.Cuisine, Name = "Cuisine" },
        new() { Id = ReferenceIds.TagTypes.CookingMethod, Name = "CookingMethod" },
        new() { Id = ReferenceIds.TagTypes.Diet, Name = "Diet" },
    ];

    // Отдельные константы на каждый тег не нужны: код на конкретные теги не завязан.
    public static readonly Tag[] Tags = BuildTags(
        (ReferenceIds.TagTypes.MealType, ["Завтрак", "Обед", "Ужин", "Перекус", "Десерт"]),
        (ReferenceIds.TagTypes.Cuisine, ["Русская", "Итальянская", "Азиатская", "Кавказская", "Средиземноморская"]),
        (ReferenceIds.TagTypes.CookingMethod, ["Варка", "Жарка", "Запекание", "Тушение", "На пару", "Гриль", "Без готовки"]),
        (ReferenceIds.TagTypes.Diet, ["Вегетарианское", "Веганское", "Высокобелковое", "Низкоуглеводное", "Без глютена"]));

    // Id = 00000007-0000-0000-<номер группы>-<номер тега в группе>. Новые теги дописывать
    // только в конец своей группы, новые группы — в конец списка, иначе сдвинутся Id существующих.
    private static Tag[] BuildTags(params (Guid TagTypeId, string[] Names)[] groups) =>
        groups
            .SelectMany((g, groupIndex) => g.Names.Select((name, tagIndex) => new Tag
            {
                Id = new Guid($"00000007-0000-0000-{groupIndex + 1:D4}-{tagIndex + 1:D12}"),
                Name = name,
                TagTypeId = g.TagTypeId,
            }))
            .ToArray();
}
