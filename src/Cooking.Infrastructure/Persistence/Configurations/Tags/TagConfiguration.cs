using Cooking.Domain.Entities.Tags;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Cooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Tags;

public class TagConfiguration : ReferenceEntityConfiguration<Tag>
{
    public override void Configure(EntityTypeBuilder<Tag> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.TagType)
            .WithMany()
            .HasForeignKey(x => x.TagTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(ReferenceDataSeed.Tags);
    }
}
