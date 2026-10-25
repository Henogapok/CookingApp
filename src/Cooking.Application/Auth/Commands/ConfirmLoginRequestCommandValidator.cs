using FluentValidation;

namespace Cooking.Application.Auth.Commands;

public class ConfirmLoginRequestCommandValidator : AbstractValidator<ConfirmLoginRequestCommand>
{
    public ConfirmLoginRequestCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
