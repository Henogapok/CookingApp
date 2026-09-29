using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Queries;

public class GetRecipeDraftQueryValidator : AbstractValidator<GetRecipeDraftQuery>
{
    public GetRecipeDraftQueryValidator()
    {
        RuleFor(x => x.DraftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
