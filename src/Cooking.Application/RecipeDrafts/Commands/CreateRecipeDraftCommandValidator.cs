using FluentValidation;

namespace Cooking.Application.RecipeDrafts.Commands;

public class CreateRecipeDraftCommandValidator : AbstractValidator<CreateRecipeDraftCommand>
{
    /// <summary>С запасом: сообщение Telegram — до 4096 символов, в PWA можно вставить текст подлиннее.</summary>
    public const int MaxTextLength = 20_000;

    public CreateRecipeDraftCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxTextLength);
    }
}
