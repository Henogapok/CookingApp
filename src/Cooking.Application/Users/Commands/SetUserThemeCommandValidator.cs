using FluentValidation;

namespace Cooking.Application.Users.Commands;

public class SetUserThemeCommandValidator : AbstractValidator<SetUserThemeCommand>
{
    public SetUserThemeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Theme)
            .NotEmpty()
            .MaximumLength(UserThemeRules.MaxLength)
            .Matches(UserThemeRules.Pattern);
    }
}
