using FluentValidation;

namespace Cooking.Application.ReferenceData.SourceTypes.Commands;

public class UpdateSourceTypeCommandValidator : AbstractValidator<UpdateSourceTypeCommand>
{
    public UpdateSourceTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
