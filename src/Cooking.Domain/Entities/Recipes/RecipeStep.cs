using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Recipes;

public class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    /// <summary>Порядок шага (1, 2, 3...).</summary>
    public int StepNumber { get; set; }
    public required string Instruction { get; set; }

    /// <summary>Если заполнено, PWA показывает кнопку таймера.</summary>
    public int? TimerSeconds { get; set; }
}
