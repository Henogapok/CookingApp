using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Recipes;

namespace Cooking.Domain.Entities.Users;

public class Family : BaseEntity
{
    public required string Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
