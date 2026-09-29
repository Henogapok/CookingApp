using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CorrectRecipeDraftCommandValidator : AbstractValidator<CorrectRecipeDraftCommand>
{
    public const int MaxTextLength = 2000;

    public CorrectRecipeDraftCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxTextLength);
    }
}
