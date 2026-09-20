using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Ingredients;

public class IngredientCatalogConfiguration : BaseEntityConfiguration<IngredientCatalog>
{
    public override void Configure(EntityTypeBuilder<IngredientCatalog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.PricePer100g).HasPrecision(10, 2);
        builder.Property(x => x.CaloriesPer100g).HasPrecision(10, 2);
        builder.Property(x => x.ProteinPer100g).HasPrecision(10, 2);
        builder.Property(x => x.FatPer100g).HasPrecision(10, 2);
        builder.Property(x => x.CarbsPer100g).HasPrecision(10, 2);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedBySource)
            .WithMany()
            .HasForeignKey(x => x.CreatedBySourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NutritionSource)
            .WithMany()
            .HasForeignKey(x => x.NutritionSourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
