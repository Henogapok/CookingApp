using FluentValidation;

namespace Cooking.Application.Recipes;

/// <summary>Общие правила для создания и обновления рецепта.</summary>
public class RecipeFieldsValidator : AbstractValidator<RecipeFields>
{
    public RecipeFieldsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.SourceUrl).MaximumLength(2048);
        RuleFor(x => x.SourceTypeId).NotEmpty();
        RuleFor(x => x.ComplexityId).NotEmpty();
        RuleFor(x => x.Servings).GreaterThanOrEqualTo(1);
        RuleFor(x => x.CookingTimeMinutes).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Ingredients).NotNull();
        RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.IngredientCatalogId).NotEmpty();
            ingredient.RuleFor(i => i.UnitId).NotEmpty();
            ingredient.RuleFor(i => i.Amount).GreaterThan(0);
        });

        RuleFor(x => x.Steps).NotNull();
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Instruction).NotEmpty().MaximumLength(4000);
            step.RuleFor(s => s.TimerSeconds).GreaterThan(0).When(s => s.TimerSeconds is not null);
        });

        RuleFor(x => x.TagIds).NotNull();
        RuleFor(x => x.TagIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.TagIds is not null)
            .WithMessage("Tags must not repeat.");
        RuleForEach(x => x.TagIds).NotEmpty();
    }
}
