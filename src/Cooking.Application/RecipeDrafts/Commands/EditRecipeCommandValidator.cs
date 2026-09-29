using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class EditRecipeCommandValidator : AbstractValidator<EditRecipeCommand>
{
    public EditRecipeCommandValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(CorrectRecipeDraftCommandValidator.MaxTextLength);
    }
}
