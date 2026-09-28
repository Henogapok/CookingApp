using FluentValidation;

namespace Cooking.Application.Recipes.Queries;

public class GetRecipeByIdQueryValidator : AbstractValidator<GetRecipeByIdQuery>
{
    public GetRecipeByIdQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
