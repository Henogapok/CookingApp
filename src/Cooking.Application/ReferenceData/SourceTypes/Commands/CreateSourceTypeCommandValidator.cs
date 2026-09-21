using FluentValidation;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public class CreateSourceTypeCommandValidator : AbstractValidator<CreateSourceTypeCommand>
{
    public CreateSourceTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
