using Cooking.Domain.Entities.Recipes;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Recipes;

public class RecipeIngredientConfiguration : BaseEntityConfiguration<RecipeIngredient>
{
    public override void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Amount).HasPrecision(10, 2);

        builder.HasOne(x => x.IngredientCatalog)
            .WithMany(x => x.RecipeIngredients)
            .HasForeignKey(x => x.IngredientCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
