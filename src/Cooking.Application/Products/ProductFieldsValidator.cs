using Cooking.Domain.ReferenceData;
using FluentValidation;

namespace Cooking.Application.Products;

/// <summary>Общие правила для создания и изменения продукта.</summary>
public class ProductFieldsValidator : AbstractValidator<ProductFields>
{
    /// <summary>В каких единицах бывает упаковка: развес и штуки, без ложек и щепоток.</summary>
    public static readonly IReadOnlySet<Guid> PackageUnits = new HashSet<Guid>
    {
        ReferenceIds.MeasurementUnits.Gram,
        ReferenceIds.MeasurementUnits.Kilogram,
        ReferenceIds.MeasurementUnits.Milliliter,
        ReferenceIds.MeasurementUnits.Liter,
        ReferenceIds.MeasurementUnits.Piece,
    };

    public ProductFieldsValidator()
    {
        RuleFor(x => x.IngredientCatalogId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x)
            .Must(x => (x.PackagePrice is null) == (x.PackageAmount is null) && (x.PackageAmount is null) == (x.PackageUnitId is null))
            .WithName("Package")
            .WithMessage("Package price, amount and unit must be set together.");
        RuleFor(x => x.PackagePrice).GreaterThan(0).LessThanOrEqualTo(10_000_000).When(x => x.PackagePrice is not null);
        RuleFor(x => x.PackageAmount).GreaterThan(0).LessThanOrEqualTo(100_000).When(x => x.PackageAmount is not null);
        RuleFor(x => x.PackageUnitId)
            .Must(id => PackageUnits.Contains(id!.Value))
            .When(x => x.PackageUnitId is not null)
            .WithMessage("Package unit must be g, kg, ml, l or pcs.");

        RuleFor(x => x)
            .Must(x => new[] { x.CaloriesPer100G, x.ProteinPer100G, x.FatPer100G, x.CarbsPer100G }.Count(v => v is null) is 0 or 4)
            .WithName("Nutrition")
            .WithMessage("Calories, protein, fat and carbs must be set together.");
        RuleFor(x => x.CaloriesPer100G).InclusiveBetween(0, 900).When(x => x.CaloriesPer100G is not null);
        RuleFor(x => x.ProteinPer100G).InclusiveBetween(0, 100).When(x => x.ProteinPer100G is not null);
        RuleFor(x => x.FatPer100G).InclusiveBetween(0, 100).When(x => x.FatPer100G is not null);
        RuleFor(x => x.CarbsPer100G).InclusiveBetween(0, 100).When(x => x.CarbsPer100G is not null);
        RuleFor(x => x)
            .Must(x => (x.ProteinPer100G ?? 0) + (x.FatPer100G ?? 0) + (x.CarbsPer100G ?? 0) <= 100)
            .WithName("Nutrition")
            .WithMessage("Protein, fat and carbs per 100 g cannot exceed 100 g together.");
    }
}
