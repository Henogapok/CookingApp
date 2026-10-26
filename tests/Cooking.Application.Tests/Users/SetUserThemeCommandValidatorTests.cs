using Cooking.Application.Users.Commands;

namespace Cooking.Application.Tests.Users;

public class SetUserThemeCommandValidatorTests
{
    private static readonly SetUserThemeCommandValidator Validator = new();

    [Theory]
    [InlineData("auto")]
    [InlineData("sage")]
    [InlineData("midnight")]
    public void ThemeId_PassesValidation(string theme)
    {
        Assert.True(Validator.Validate(new SetUserThemeCommand(Guid.NewGuid(), theme)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Sage")]
    [InlineData("dark theme")]
    [InlineData("<script>")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefg")]
    public void NotThemeId_FailsValidation(string theme)
    {
        Assert.False(Validator.Validate(new SetUserThemeCommand(Guid.NewGuid(), theme)).IsValid);
    }
}
