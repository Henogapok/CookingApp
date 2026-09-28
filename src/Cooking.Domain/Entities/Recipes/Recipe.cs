using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;

namespace Cooking.Domain.Entities.Recipes;

public class Recipe : BaseEntity
{
    /// <summary>Владелец рецепта. Семья автора видит рецепт через его членство в Family.</summary>
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public required string Title { get; set; }
    public string? Description { get; set; }

    /// <summary>URL видео или сайта, откуда взят рецепт.</summary>
    public string? SourceUrl { get; set; }

    public Guid SourceTypeId { get; set; }
    public SourceType SourceType { get; set; } = null!;

    public Guid ComplexityId { get; set; }
    public Complexity Complexity { get; set; } = null!;

    public int Servings { get; set; }
    public int CookingTimeMinutes { get; set; }

    // КБЖУ и стоимость не хранятся: считаются из IngredientCatalog (позже — через view в БД),
    // чтобы изменение цены/КБЖУ ингредиента сразу отражалось во всех рецептах.

    /// <summary>Soft delete: не null — рецепт удалён и скрыт глобальным query filter'ом.</summary>
    public DateTime? DeletedAt { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
}
