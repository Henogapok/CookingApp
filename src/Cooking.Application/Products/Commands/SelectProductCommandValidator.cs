using FluentValidation;

namespace Cooking.Application.Products.Commands;

public class SelectProductCommandValidator : AbstractValidator<SelectProductCommand>
{
    public SelectProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
