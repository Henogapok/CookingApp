using FluentValidation;

namespace Cooking.Application.MeasurementUnits.Commands;

public class CreateMeasurementUnitCommandValidator : AbstractValidator<CreateMeasurementUnitCommand>
{
    public CreateMeasurementUnitCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Abbreviation)
            .NotEmpty()
            .MaximumLength(20);
    }
}
