using FluentValidation;

namespace Cooking.Application.Ingredients.Commands;

public class UpdateIngredientCatalogCommandValidator : AbstractValidator<UpdateIngredientCatalogCommand>
{
    public UpdateIngredientCatalogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.CreatedBySourceId).NotEmpty();
        RuleFor(x => x.NutritionSourceId).NotEmpty();
        RuleFor(x => x.PricePer100G).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CaloriesPer100G).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProteinPer100G).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FatPer100G).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CarbsPer100G).GreaterThanOrEqualTo(0);
    }
}
