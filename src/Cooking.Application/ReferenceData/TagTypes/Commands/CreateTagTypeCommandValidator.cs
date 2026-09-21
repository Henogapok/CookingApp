using FluentValidation;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public class CreateTagTypeCommandValidator : AbstractValidator<CreateTagTypeCommand>
{
    public CreateTagTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
