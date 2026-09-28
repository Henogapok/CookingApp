using FluentValidation;

namespace Cooking.Application.Recipes.Queries;

public class GetRecipesQueryValidator : AbstractValidator<GetRecipesQuery>
{
    public GetRecipesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
