using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class SelectRecipeDraftDishesCommandValidator : AbstractValidator<SelectRecipeDraftDishesCommand>
{
    public SelectRecipeDraftDishesCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DishIndexes)
            .NotEmpty()
            .Must(x => x.Distinct().Count() <= RecipeDraftLimits.MaxDishes)
            .WithMessage($"Select at most {RecipeDraftLimits.MaxDishes} dishes.");
        RuleForEach(x => x.DishIndexes).GreaterThanOrEqualTo(0);
    }
}
