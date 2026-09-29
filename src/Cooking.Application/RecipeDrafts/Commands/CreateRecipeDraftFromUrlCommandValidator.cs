using Cooking.Application.RecipeDrafts.Sources;
using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftFromUrlCommandValidator : AbstractValidator<CreateRecipeDraftFromUrlCommand>
{
    public CreateRecipeDraftFromUrlCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(url => RecipeSourceText.FindInstagramLink(url) is not null)
            .WithMessage("Only Instagram post/Reels links are supported.");
    }
}
