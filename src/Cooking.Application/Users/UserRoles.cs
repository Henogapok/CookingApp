namespace Cooking.Application.Users;

/// <summary>
/// Глобальные роли пользователя — значения claim role в access token. Выдаёт их Cooking.Auth, проверяет Cooking.Api
/// через политики. Права на конкретные записи (рецепт видят автор и семья) — не роли, их проверяют репозитории.
/// </summary>
public static class UserRoles
{
    /// <summary>Claim с ролями в access token (стандартное имя из OpenID Connect, у OpenIddict — Claims.Role).</summary>
    public const string ClaimType = "role";

    /// <summary>Правит общие справочники и каталог ингредиентов.</summary>
    public const string Admin = "admin";
}
