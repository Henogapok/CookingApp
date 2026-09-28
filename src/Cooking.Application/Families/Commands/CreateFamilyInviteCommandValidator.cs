using FluentValidation;

namespace Cooking.Application.Families.Commands;

public class CreateFamilyInviteCommandValidator : AbstractValidator<CreateFamilyInviteCommand>
{
    public CreateFamilyInviteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
