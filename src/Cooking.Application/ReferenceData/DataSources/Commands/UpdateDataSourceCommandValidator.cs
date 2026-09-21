using FluentValidation;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public class UpdateDataSourceCommandValidator : AbstractValidator<UpdateDataSourceCommand>
{
    public UpdateDataSourceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
