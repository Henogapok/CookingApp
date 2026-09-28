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

        // Soft delete: удалённые рецепты не попадают ни в один запрос.
        // Обойти фильтр (например, для восстановления) — IgnoreQueryFilters().
        builder.HasQueryFilter(x => x.DeletedAt == null);

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
