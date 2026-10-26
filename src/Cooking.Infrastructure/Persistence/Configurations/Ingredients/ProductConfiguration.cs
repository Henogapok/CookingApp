using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Ingredients;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PackagePrice).HasPrecision(12, 2);
        builder.Property(x => x.PackageAmount).HasPrecision(12, 3);
        builder.Property(x => x.CaloriesPer100g).HasPrecision(10, 2);
        builder.Property(x => x.ProteinPer100g).HasPrecision(10, 2);
        builder.Property(x => x.FatPer100g).HasPrecision(10, 2);
        builder.Property(x => x.CarbsPer100g).HasPrecision(10, 2);

        // Продукты ищут по ингредиенту среди продуктов семьи.
        builder.HasIndex(x => new { x.IngredientCatalogId, x.CreatedByUserId });

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Без ингредиента продукт ничего не значит: удалили ингредиент из каталога — уходят и его продукты.
        builder.HasOne(x => x.IngredientCatalog)
            .WithMany()
            .HasForeignKey(x => x.IngredientCatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PackageUnit)
            .WithMany()
            .HasForeignKey(x => x.PackageUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
