using FluentValidation;

namespace Cooking.Application.Products.Queries;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
