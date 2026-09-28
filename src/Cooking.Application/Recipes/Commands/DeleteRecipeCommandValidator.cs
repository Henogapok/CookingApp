using FluentValidation;

namespace Cooking.Application.Recipes.Commands;

public class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
