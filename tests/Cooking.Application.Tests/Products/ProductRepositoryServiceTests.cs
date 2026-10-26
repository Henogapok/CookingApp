using Cooking.Application.Common.Errors;
using Cooking.Application.Ingredients;
using Cooking.Application.Products;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Users;
using Cooking.Domain.ReferenceData;
using Cooking.Infrastructure.Persistence;
using Cooking.Infrastructure.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Cooking.Application.Tests.Products;

public class ProductRepositoryServiceTests
{
    /// <summary>Иван и Маша — одна семья, Пётр — посторонний. У Ивана рецепт с молоком, рисом и морковью.</summary>
    private sealed record Setup(Guid IvanId, Guid MashaId, Guid PetyaId, Guid MilkId, Guid RiceId, Guid CarrotId, Guid EggsId, Guid RecipeId);

    private static readonly Guid Gram = ReferenceIds.MeasurementUnits.Gram;
    private static readonly Guid Kilogram = ReferenceIds.MeasurementUnits.Kilogram;
    private static readonly Guid Liter = ReferenceIds.MeasurementUnits.Liter;
    private static readonly Guid Piece = ReferenceIds.MeasurementUnits.Piece;

    private static CookingDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Setup> SeedAsync(CookingDbContext context)
    {
        var family = new Family { Name = "Ивановы" };
        var ivan = new User { TelegramId = 1, FirstName = "Иван", Family = family };
        var masha = new User { TelegramId = 2, FirstName = "Маша", Family = family };
        var petya = new User { TelegramId = 3, FirstName = "Пётр" };

        // Id из ReferenceIds: по ним NutritionCalculator пересчитывает упаковку.
        var gram = new MeasurementUnit { Id = Gram, Name = "Грамм", Abbreviation = "г" };
        var ml = new MeasurementUnit { Id = ReferenceIds.MeasurementUnits.Milliliter, Name = "Миллилитр", Abbreviation = "мл" };
        var kg = new MeasurementUnit { Id = Kilogram, Name = "Килограмм", Abbreviation = "кг" };
        var liter = new MeasurementUnit { Id = Liter, Name = "Литр", Abbreviation = "л" };
        var piece = new MeasurementUnit { Id = Piece, Name = "Штука", Abbreviation = "шт" };
        var category = new IngredientCategory { Name = "Разное" };
        var dataSource = new DataSource { Name = "Manual" };

        IngredientCatalog Ingredient(string name, MeasurementUnit baseUnit, decimal calories, decimal? pieceWeight = null) => new()
        {
            Name = name,
            Category = category,
            BaseUnit = baseUnit,
            PieceWeight = pieceWeight,
            CaloriesPer100g = calories,
            CreatedBySource = dataSource,
            NutritionSource = dataSource,
        };

        var milk = Ingredient("Молоко 3,2%", ml, 60);
        var rice = Ingredient("Рис", gram, 345);
        var carrot = Ingredient("Морковь", gram, 35);
        var eggs = Ingredient("Яйца", gram, 157, pieceWeight: 55);
        var salt = Ingredient("Соль", gram, 0);

        var recipe = new Recipe
        {
            Title = "Рисовая каша",
            CreatedByUser = ivan,
            SourceType = new SourceType { Name = "Manual" },
            Complexity = new Complexity { Name = "Easy" },
            Servings = 2,
            Ingredients =
            [
                new RecipeIngredient { IngredientCatalog = milk, Amount = 200, UnitId = ml.Id, SortOrder = 1 },
                new RecipeIngredient { IngredientCatalog = rice, Amount = 500, UnitId = gram.Id, SortOrder = 2 },
                new RecipeIngredient { IngredientCatalog = carrot, Amount = 100, UnitId = gram.Id, SortOrder = 3 },
                new RecipeIngredient { IngredientCatalog = salt, SortOrder = 4 }, // по вкусу
            ],
        };

        context.AddRange(ivan, masha, petya, kg, liter, piece, eggs, recipe);
        await context.SaveChangesAsync(CancellationToken.None);

        return new Setup(ivan.Id, masha.Id, petya.Id, milk.Id, rice.Id, carrot.Id, eggs.Id, recipe.Id);
    }

    private static ProductFields Fields(
        Guid ingredientId, string name, decimal? price = null, decimal? amount = null, Guid? unitId = null, decimal? calories = null) =>
        new(ingredientId, name, price, amount, unitId,
            calories, calories is null ? null : 3, calories is null ? null : 3.2m, calories is null ? null : 4.7m);

    /// <summary>Сдвигает время выбора назад: создание в тесте идёт быстрее, чем тикают часы.</summary>
    private static async Task AgeSelectionAsync(CookingDbContext context, Guid productId, int minutes)
    {
        var product = await context.Products.SingleAsync(p => p.Id == productId);
        product.SelectedAt = DateTime.UtcNow.AddMinutes(-minutes);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private static void AssertError(ResultBase result, ErrorCode expected)
    {
        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(Assert.Single(result.Errors));
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task CreateAsync_NewProduct_SelectedForWholeFamily()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);

        var created = await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel", 520, 1, Liter), CancellationToken.None);

        Assert.True(created.IsSuccess);
        var milk = (await repository.GetHouseholdAsync(s.MashaId, CancellationToken.None)).Value.Single(i => i.Name == "Молоко 3,2%");
        var product = Assert.Single(milk.Products);
        Assert.True(product.IsSelected);
        Assert.Equal(52, product.PricePer100);
        Assert.Equal("л", product.PackageUnitAbbreviation);
    }

    [Fact]
    public async Task GetHouseholdAsync_ListsRecipeIngredientsWithoutProducts()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        await repository.CreateAsync(s.IvanId, Fields(s.EggsId, "Казгер С0", 950, 10, Piece), CancellationToken.None);

        var household = (await repository.GetHouseholdAsync(s.MashaId, CancellationToken.None)).Value;

        // Из рецепта — молоко, рис, морковь (соль «по вкусу» — нет); яйца — только из-за продукта.
        Assert.Equal(["Молоко 3,2%", "Морковь", "Рис", "Яйца"], household.Select(i => i.Name));
        var carrot = household.Single(i => i.Name == "Морковь");
        Assert.Equal(1, carrot.RecipeCount);
        Assert.Empty(carrot.Products);
        var eggs = household.Single(i => i.Name == "Яйца");
        Assert.Equal(0, eggs.RecipeCount);
        Assert.Equal(172.73m, Math.Round(Assert.Single(eggs.Products).PricePer100!.Value, 2)); // 950 ₸ / (10 × 55 г) × 100
    }

    [Fact]
    public async Task GetHouseholdAsync_StrangerSeesNeitherFamilyRecipesNorProducts()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel", 520, 1, Liter), CancellationToken.None);

        var household = await repository.GetHouseholdAsync(s.PetyaId, CancellationToken.None);

        Assert.Empty(household.Value);
    }

    [Fact]
    public async Task SelectAsync_LatestSelectionWins()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var lactel = (await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel", 520, 1, Liter), CancellationToken.None)).Value;
        var adal = (await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Adal", 480, 1, Liter), CancellationToken.None)).Value;
        await AgeSelectionAsync(context, lactel, minutes: 20);
        await AgeSelectionAsync(context, adal, minutes: 10);

        var result = await repository.SelectAsync(lactel, s.MashaId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var milk = (await repository.GetHouseholdAsync(s.IvanId, CancellationToken.None)).Value.Single(i => i.Name == "Молоко 3,2%");
        Assert.Equal("Lactel", Assert.Single(milk.Products, p => p.IsSelected).Name);
    }

    [Fact]
    public async Task UpdateAndDelete_ByFamilyMember_Allowed()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var id = (await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel", 520, 1, Liter), CancellationToken.None)).Value;

        var updated = await repository.UpdateAsync(id, s.MashaId, Fields(s.MilkId, "Lactel 3,2%", 540, 1, Liter, calories: 58), CancellationToken.None);
        Assert.True(updated.IsSuccess);
        var product = await context.Products.SingleAsync(p => p.Id == id);
        Assert.Equal("Lactel 3,2%", product.Name);
        Assert.Equal(58, product.CaloriesPer100g);

        var deleted = await repository.DeleteAsync(id, s.MashaId, CancellationToken.None);
        Assert.True(deleted.IsSuccess);
        Assert.False(await context.Products.AnyAsync());
    }

    [Fact]
    public async Task UpdateDeleteSelect_ByStranger_ReturnNotFound()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var id = (await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel", 520, 1, Liter), CancellationToken.None)).Value;

        AssertError(await repository.UpdateAsync(id, s.PetyaId, Fields(s.MilkId, "Чужое"), CancellationToken.None), ErrorCode.NotFound);
        AssertError(await repository.DeleteAsync(id, s.PetyaId, CancellationToken.None), ErrorCode.NotFound);
        AssertError(await repository.SelectAsync(id, s.PetyaId, CancellationToken.None), ErrorCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_OtherIngredient_FailsValidation()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var id = (await repository.CreateAsync(s.IvanId, Fields(s.MilkId, "Lactel"), CancellationToken.None)).Value;

        var result = await repository.UpdateAsync(id, s.IvanId, Fields(s.RiceId, "Lactel"), CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_PiecesWithoutPieceWeight_FailsValidation()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);

        var result = await repository.CreateAsync(s.IvanId, Fields(s.CarrotId, "С рынка", 300, 5, Piece), CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_MacrosOver100PerGram_FailsValidation()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var fields = Fields(s.RiceId, "Алтын") with { CaloriesPer100G = 400, ProteinPer100G = 40, FatPer100G = 40, CarbsPer100G = 30 };

        var result = await repository.CreateAsync(s.IvanId, fields, CancellationToken.None);

        AssertError(result, ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_MacrosOver100PerMilliliter_Allowed()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        var fields = Fields(s.MilkId, "Сгущёнка") with { CaloriesPer100G = 420, ProteinPer100G = 9, FatPer100G = 11, CarbsPer100G = 85 };

        var result = await repository.CreateAsync(s.IvanId, fields, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CatalogUpdate_RemovingPieceWeightUsedByPackages_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        await new ProductRepositoryService(context).CreateAsync(s.IvanId, Fields(s.EggsId, "Казгер С0", 950, 10, Piece), CancellationToken.None);
        var eggs = await context.IngredientCatalog.SingleAsync(i => i.Id == s.EggsId);
        var fields = new IngredientCatalogFields(
            eggs.Name, eggs.CategoryId, eggs.BaseUnitId, eggs.CaloriesPer100g, eggs.ProteinPer100g, eggs.FatPer100g, eggs.CarbsPer100g,
            eggs.CreatedBySourceId, eggs.NutritionSourceId, PieceWeight: null);

        var result = await new IngredientCatalogRepositoryService(context).UpdateAsync(s.EggsId, fields, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
        Assert.Equal(55, (await context.IngredientCatalog.SingleAsync(i => i.Id == s.EggsId)).PieceWeight);
    }

    [Fact]
    public async Task CatalogUpdate_MillilitersToGramsBreakingProductMacros_ReturnsLogicConflict()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var condensed = Fields(s.MilkId, "Сгущёнка") with { CaloriesPer100G = 420, ProteinPer100G = 9, FatPer100G = 11, CarbsPer100G = 85 };
        await new ProductRepositoryService(context).CreateAsync(s.IvanId, condensed, CancellationToken.None);
        var milk = await context.IngredientCatalog.SingleAsync(i => i.Id == s.MilkId);
        var fields = new IngredientCatalogFields(
            milk.Name, milk.CategoryId, Gram, milk.CaloriesPer100g, milk.ProteinPer100g, milk.FatPer100g, milk.CarbsPer100g,
            milk.CreatedBySourceId, milk.NutritionSourceId);

        var result = await new IngredientCatalogRepositoryService(context).UpdateAsync(s.MilkId, fields, CancellationToken.None);

        AssertError(result, ErrorCode.LogicConflict);
    }

    [Fact]
    public async Task LeavingFamily_ProductsStayWithAuthor()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var repository = new ProductRepositoryService(context);
        await repository.CreateAsync(s.MashaId, Fields(s.MilkId, "Adal", 480, 1, Liter), CancellationToken.None);

        var masha = await context.Users.SingleAsync(u => u.Id == s.MashaId);
        masha.FamilyId = null;
        await context.SaveChangesAsync(CancellationToken.None);

        var ivanHousehold = (await repository.GetHouseholdAsync(s.IvanId, CancellationToken.None)).Value;
        Assert.Empty(ivanHousehold.Single(i => i.Name == "Молоко 3,2%").Products);
        var mashaHousehold = (await repository.GetHouseholdAsync(s.MashaId, CancellationToken.None)).Value;
        Assert.Equal("Adal", Assert.Single(Assert.Single(mashaHousehold).Products).Name);
    }

    [Fact]
    public async Task RecipeCost_ByViewerSelectedProducts()
    {
        await using var context = CreateContext();
        var s = await SeedAsync(context);
        var products = new ProductRepositoryService(context);
        await products.CreateAsync(s.MashaId, Fields(s.MilkId, "Lactel", 520, 1, Liter, calories: 58), CancellationToken.None);
        await products.CreateAsync(s.IvanId, Fields(s.RiceId, "Алтын", 1500, 1, Kilogram), CancellationToken.None);

        var recipe = (await new RecipeRepositoryService(context).GetByIdAsync(s.RecipeId, s.IvanId, CancellationToken.None)).Value;

        // 200 мл × 52 ₸/100 + 500 г × 150 ₸/100
        Assert.Equal(104 + 750, recipe.Nutrition.Cost);
        Assert.Equal(["Морковь"], recipe.Nutrition.IngredientsWithoutPrice);
        var milk = recipe.Ingredients.Single(i => i.IngredientName == "Молоко 3,2%");
        Assert.Equal("Lactel", milk.ProductName);
        Assert.Equal(116, milk.Nutrition!.Calories); // КБЖУ с упаковки: 200 мл × 58 / 100
    }
}
