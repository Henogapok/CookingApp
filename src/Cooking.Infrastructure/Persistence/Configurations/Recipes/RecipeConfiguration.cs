using Cooking.Domain.Entities.Recipes;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Recipes;

public class RecipeConfiguration : BaseEntityConfiguration<Recipe>
{
    public override void Configure(EntityTypeBuilder<Recipe> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);

        builder.Property(x => x.TotalCalories).HasPrecision(10, 2);
        builder.Property(x => x.TotalProtein).HasPrecision(10, 2);
        builder.Property(x => x.TotalFat).HasPrecision(10, 2);
        builder.Property(x => x.TotalCarbs).HasPrecision(10, 2);
        builder.Property(x => x.EstimatedCost).HasPrecision(10, 2);

        builder.HasOne(x => x.SourceType)
            .WithMany()
            .HasForeignKey(x => x.SourceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Complexity)
            .WithMany()
            .HasForeignKey(x => x.ComplexityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Ingredients)
            .WithOne(x => x.Recipe)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Steps)
            .WithOne(x => x.Recipe)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
