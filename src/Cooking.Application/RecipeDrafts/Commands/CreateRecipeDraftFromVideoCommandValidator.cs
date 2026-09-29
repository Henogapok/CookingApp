using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftFromVideoCommandValidator : AbstractValidator<CreateRecipeDraftFromVideoCommand>
{
    public CreateRecipeDraftFromVideoCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.VideoFilePath).NotEmpty();
        RuleFor(x => x.Caption).MaximumLength(CreateRecipeDraftCommandValidator.MaxTextLength);
    }
}
