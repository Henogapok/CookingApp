using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Users;

/// <summary>
/// Группа пользователей, внутри которой рецепты общие. Рецепты принадлежат автору (User),
/// а не семье — поэтому вступление/выход из семьи ничего не переносит.
/// </summary>
public class Family : BaseEntity
{
    public required string Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<FamilyInvite> Invites { get; set; } = new List<FamilyInvite>();
}
