using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ConfirmRecipeDraftCommandValidator : AbstractValidator<ConfirmRecipeDraftCommand>
{
    public ConfirmRecipeDraftCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
