using FluentValidation;

namespace Cooking.Application.MeasurementUnits.Commands;

public class UpdateMeasurementUnitCommandValidator : AbstractValidator<UpdateMeasurementUnitCommand>
{
    public UpdateMeasurementUnitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Abbreviation)
            .NotEmpty()
            .MaximumLength(20);
    }
}
