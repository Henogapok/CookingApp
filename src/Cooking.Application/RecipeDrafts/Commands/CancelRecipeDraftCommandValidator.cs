using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CancelRecipeDraftCommandValidator : AbstractValidator<CancelRecipeDraftCommand>
{
    public CancelRecipeDraftCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
