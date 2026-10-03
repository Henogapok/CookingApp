using FluentValidation;

namespace Cooking.Application.Auth.Commands;

public class CompleteLoginRequestCommandValidator : AbstractValidator<CompleteLoginRequestCommand>
{
    public CompleteLoginRequestCommandValidator()
    {
        RuleFor(x => x.PollToken).NotEmpty().MaximumLength(128);
    }
}
