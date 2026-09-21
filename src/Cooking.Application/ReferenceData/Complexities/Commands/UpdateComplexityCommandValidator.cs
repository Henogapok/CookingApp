using FluentValidation;

namespace Cooking.Application.ReferenceData.Complexities.Commands;

public class UpdateComplexityCommandValidator : AbstractValidator<UpdateComplexityCommand>
{
    public UpdateComplexityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
