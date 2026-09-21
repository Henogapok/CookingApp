using FluentValidation;

namespace Cooking.Application.ReferenceData.DataSources.Commands;

public class CreateDataSourceCommandValidator : AbstractValidator<CreateDataSourceCommand>
{
    public CreateDataSourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
