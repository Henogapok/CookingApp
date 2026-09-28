using Cooking.Domain.Entities.Tags;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Cooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Tags;

public class TagTypeConfiguration : ReferenceEntityConfiguration<TagType>
{
    public override void Configure(EntityTypeBuilder<TagType> builder)
    {
        base.Configure(builder);

        builder.HasData(ReferenceDataSeed.TagTypes);
    }
}
