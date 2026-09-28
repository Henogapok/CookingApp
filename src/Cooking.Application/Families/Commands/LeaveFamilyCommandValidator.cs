using FluentValidation;

namespace Cooking.Application.Families.Commands;

public class LeaveFamilyCommandValidator : AbstractValidator<LeaveFamilyCommand>
{
    public LeaveFamilyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
