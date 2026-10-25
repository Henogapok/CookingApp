using FluentValidation;

namespace Cooking.Application.Auth.Commands;

public class DeclineLoginRequestCommandValidator : AbstractValidator<DeclineLoginRequestCommand>
{
    public DeclineLoginRequestCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
    }
}
