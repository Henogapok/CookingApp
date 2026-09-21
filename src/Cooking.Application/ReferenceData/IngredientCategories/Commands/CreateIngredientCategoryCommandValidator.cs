using FluentValidation;

namespace Cooking.Application.ReferenceData.IngredientCategories.Commands;

public class CreateIngredientCategoryCommandValidator : AbstractValidator<CreateIngredientCategoryCommand>
{
    public CreateIngredientCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
