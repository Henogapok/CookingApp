using Cooking.Application.Products;
using Cooking.Domain.ReferenceData;

namespace Cooking.Application.Tests.Products;

public class ProductFieldsValidatorTests
{
    private static readonly ProductFieldsValidator Validator = new();

    private static readonly ProductFields Valid = new(
        Guid.NewGuid(), "Lactel 3,2%", 520, 1, ReferenceIds.MeasurementUnits.Liter, 58, 2.9m, 3.2m, 4.7m);

    [Fact]
    public void FullProduct_PassesValidation()
    {
        Assert.True(Validator.Validate(Valid).IsValid);
    }

    [Fact]
    public void NoPriceAndNoNutrition_PassesValidation()
    {
        Assert.True(Validator.Validate(Valid with
        {
            PackagePrice = null, PackageAmount = null, PackageUnitId = null,
            CaloriesPer100G = null, ProteinPer100G = null, FatPer100G = null, CarbsPer100G = null,
        }).IsValid);
    }

    [Fact]
    public void PriceWithoutPackage_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { PackageAmount = null, PackageUnitId = null }).IsValid);
    }

    [Fact]
    public void PartialNutrition_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { FatPer100G = null }).IsValid);
    }

    [Fact]
    public void SpoonAsPackageUnit_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { PackageUnitId = ReferenceIds.MeasurementUnits.Tablespoon }).IsValid);
    }

    [Fact]
    public void ZeroPrice_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { PackagePrice = 0 }).IsValid);
    }

    [Fact]
    public void HoneyPer100Ml_PassesValidation()
    {
        // Мёд: ~110 г углеводов на 100 мл — для мл это нормально, «Б+Ж+У ≤ 100» проверяется только для граммов.
        Assert.True(Validator.Validate(Valid with { CaloriesPer100G = 450, ProteinPer100G = 1, FatPer100G = 0, CarbsPer100G = 110 }).IsValid);
    }

    [Fact]
    public void PackageAmountWithGramPrecision_PassesValidation()
    {
        Assert.True(Validator.Validate(Valid with { PackageAmount = 0.125m, PackageUnitId = ReferenceIds.MeasurementUnits.Kilogram }).IsValid);
    }

    [Fact]
    public void PackageAmountFinerThanStored_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { PackageAmount = 0.1234m }).IsValid);
    }

    [Fact]
    public void NutritionFinerThanStored_FailsValidation()
    {
        Assert.False(Validator.Validate(Valid with { ProteinPer100G = 3.255m }).IsValid);
    }
}
