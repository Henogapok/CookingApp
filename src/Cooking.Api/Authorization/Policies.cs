using Cooking.Application.Users;
using Microsoft.AspNetCore.Authorization;

namespace Cooking.Api.Authorization;

/// <summary>
/// Политики доступа. Контроллер говорит, ЧТО нужно (<c>[Authorize(Policy = Policies.ManageReferenceData)]</c>), а кто это может —
/// решается только здесь: появится новая роль или разрешения из базы — меняется определение политики, а не контроллеры.
/// </summary>
public static class Policies
{
    /// <summary>Запись в общие справочники и каталог ингредиентов: они общие для всех пользователей и семей.</summary>
    public const string ManageReferenceData = nameof(ManageReferenceData);

    public static void Add(AuthorizationOptions options)
    {
        options.AddPolicy(ManageReferenceData, policy => policy.RequireRole(UserRoles.Admin));
    }
}
