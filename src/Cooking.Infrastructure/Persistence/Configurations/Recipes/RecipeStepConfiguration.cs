using Cooking.Domain.Entities.Recipes;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Recipes;

public class RecipeStepConfiguration : BaseEntityConfiguration<RecipeStep>
{
    public override void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Instruction)
            .IsRequired()
            .HasMaxLength(4000);
    }
}
