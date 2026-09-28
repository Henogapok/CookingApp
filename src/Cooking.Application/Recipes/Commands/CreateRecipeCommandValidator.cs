using FluentValidation;

namespace Cooking.Application.Recipes.Commands;

public class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Recipe).NotNull().SetValidator(new RecipeFieldsValidator());
    }
}
