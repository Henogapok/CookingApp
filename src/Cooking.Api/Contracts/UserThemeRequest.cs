using Cooking.Application.Users.Commands;

namespace Cooking.Api.Contracts;

/// <summary>Тело PUT api/users/me/theme: id темы PWA или «auto».</summary>
public record UserThemeRequest(string Theme)
{
    public SetUserThemeCommand ToCommand(Guid userId) => new(userId, Theme);
}
