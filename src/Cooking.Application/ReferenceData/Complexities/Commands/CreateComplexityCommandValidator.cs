using FluentValidation;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public class CreateComplexityCommandValidator : AbstractValidator<CreateComplexityCommand>
{
    public CreateComplexityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
