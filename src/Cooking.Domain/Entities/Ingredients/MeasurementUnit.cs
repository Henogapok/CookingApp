using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Ingredients;

/// <summary>Единица измерения: г, мл, шт, ст.л., ч.л. и т.д.</summary>
public class MeasurementUnit : ReferenceEntity
{
    public required string Abbreviation { get; set; }
}
