using FluentValidation;

namespace Cooking.Application.Recipes.Commands;

public class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Recipe).NotNull().SetValidator(new RecipeFieldsValidator());
    }
}
