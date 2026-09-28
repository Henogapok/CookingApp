using Cooking.Domain.Entities.Recipes;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Cooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Recipes;

public class SourceTypeConfiguration : ReferenceEntityConfiguration<SourceType>
{
    public override void Configure(EntityTypeBuilder<SourceType> builder)
    {
        base.Configure(builder);

        builder.HasData(ReferenceDataSeed.SourceTypes);
    }
}
