namespace Cooking.Domain.ReferenceData;

/// <summary>
/// Фиксированные Id справочников, которые заполняются seed'ом в миграциях.
/// Код может ссылаться на них напрямую (например, SourceTypes.Manual при создании рецепта ботом),
/// не ища запись по названию. Формат: первый блок — номер справочника, последний — номер записи.
/// </summary>
public static class ReferenceIds
{
    public static class SourceTypes
    {
        public static readonly Guid Manual = new("00000001-0000-0000-0000-000000000001");
        public static readonly Guid Instagram = new("00000001-0000-0000-0000-000000000002");
        public static readonly Guid YouTube = new("00000001-0000-0000-0000-000000000003");
        public static readonly Guid Website = new("00000001-0000-0000-0000-000000000004");
    }

    public static class Complexities
    {
        public static readonly Guid Easy = new("00000002-0000-0000-0000-000000000001");
        public static readonly Guid Medium = new("00000002-0000-0000-0000-000000000002");
        public static readonly Guid Hard = new("00000002-0000-0000-0000-000000000003");
    }

    public static class MeasurementUnits
    {
        public static readonly Guid Gram = new("00000003-0000-0000-0000-000000000001");
        public static readonly Guid Milliliter = new("00000003-0000-0000-0000-000000000002");
        public static readonly Guid Piece = new("00000003-0000-0000-0000-000000000003");
        public static readonly Guid Tablespoon = new("00000003-0000-0000-0000-000000000004");
        public static readonly Guid Teaspoon = new("00000003-0000-0000-0000-000000000005");
        public static readonly Guid Glass = new("00000003-0000-0000-0000-000000000006");
        public static readonly Guid Pinch = new("00000003-0000-0000-0000-000000000007");
    }

    public static class DataSources
    {
        public static readonly Guid Manual = new("00000004-0000-0000-0000-000000000001");
        public static readonly Guid Llm = new("00000004-0000-0000-0000-000000000002");
        public static readonly Guid FatSecret = new("00000004-0000-0000-0000-000000000003");
    }

    public static class IngredientCategories
    {
        public static readonly Guid Meat = new("00000005-0000-0000-0000-000000000001");
        public static readonly Guid Poultry = new("00000005-0000-0000-0000-000000000002");
        public static readonly Guid Seafood = new("00000005-0000-0000-0000-000000000003");
        public static readonly Guid Vegetables = new("00000005-0000-0000-0000-000000000004");
        public static readonly Guid Fruits = new("00000005-0000-0000-0000-000000000005");
        public static readonly Guid GrainsAndLegumes = new("00000005-0000-0000-0000-000000000006");
        public static readonly Guid Dairy = new("00000005-0000-0000-0000-000000000007");
        public static readonly Guid Eggs = new("00000005-0000-0000-0000-000000000008");
        public static readonly Guid OilsAndFats = new("00000005-0000-0000-0000-000000000009");
        public static readonly Guid FlourAndBakery = new("00000005-0000-0000-0000-000000000010");
        public static readonly Guid Spices = new("00000005-0000-0000-0000-000000000011");
        public static readonly Guid Drinks = new("00000005-0000-0000-0000-000000000012");
    }

    public static class TagTypes
    {
        public static readonly Guid MealType = new("00000006-0000-0000-0000-000000000001");
        public static readonly Guid Cuisine = new("00000006-0000-0000-0000-000000000002");
        public static readonly Guid CookingMethod = new("00000006-0000-0000-0000-000000000003");
        public static readonly Guid Diet = new("00000006-0000-0000-0000-000000000004");
    }
}
