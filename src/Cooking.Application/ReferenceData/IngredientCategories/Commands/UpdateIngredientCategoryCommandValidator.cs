using FluentValidation;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public class UpdateIngredientCategoryCommandValidator : AbstractValidator<UpdateIngredientCategoryCommand>
{
    public UpdateIngredientCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
