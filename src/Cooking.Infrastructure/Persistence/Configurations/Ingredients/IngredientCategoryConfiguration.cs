using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Cooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Ingredients;

public class IngredientCategoryConfiguration : ReferenceEntityConfiguration<IngredientCategory>
{
    public override void Configure(EntityTypeBuilder<IngredientCategory> builder)
    {
        base.Configure(builder);

        builder.HasData(ReferenceDataSeed.IngredientCategories);
    }
}
