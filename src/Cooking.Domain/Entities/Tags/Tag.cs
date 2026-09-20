using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Tags;

public class Tag : ReferenceEntity
{
    public Guid TagTypeId { get; set; }
    public TagType TagType { get; set; } = null!;

    public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
}
