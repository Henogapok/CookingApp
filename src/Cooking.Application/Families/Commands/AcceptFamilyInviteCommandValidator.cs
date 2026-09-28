using FluentValidation;

namespace Cooking.Application.Families.Commands;

public class AcceptFamilyInviteCommandValidator : AbstractValidator<AcceptFamilyInviteCommand>
{
    public AcceptFamilyInviteCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
