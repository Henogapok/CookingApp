using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class ApplyRecipeDraftEstimatesCommandValidator : AbstractValidator<ApplyRecipeDraftEstimatesCommand>
{
    public ApplyRecipeDraftEstimatesCommandValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
