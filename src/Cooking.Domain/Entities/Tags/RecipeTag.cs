using Cooking.Domain.Entities.Recipes;

namespace Cooking.Domain.Entities.Tags;

/// <summary>Промежуточная таблица many-to-many между Recipe и Tag. Составной ключ (RecipeId, TagId).</summary>
public class RecipeTag
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
