using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Tags;
using Cooking.Domain.Entities.Users;

namespace Cooking.Domain.Entities.Recipes;

public class Recipe : BaseEntity
{
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;

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

    /// <summary>КБЖУ и стоимость — денормализованные суммы, пересчитываются из IngredientCatalog.</summary>
    public decimal TotalCalories { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalFat { get; set; }
    public decimal TotalCarbs { get; set; }
    public decimal EstimatedCost { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
}
