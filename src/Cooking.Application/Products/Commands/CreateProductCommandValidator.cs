using FluentValidation;

namespace Cooking.Application.Products.Commands;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Product).NotNull().SetValidator(new ProductFieldsValidator());
    }
}
