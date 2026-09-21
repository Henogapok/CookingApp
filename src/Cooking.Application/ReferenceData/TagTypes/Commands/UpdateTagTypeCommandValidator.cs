using FluentValidation;

namespace Cooking.Application.ReferenceData.TagTypes.Commands;

public class UpdateTagTypeCommandValidator : AbstractValidator<UpdateTagTypeCommand>
{
    public UpdateTagTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
