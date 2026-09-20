using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Users;

public class FamilyConfiguration : BaseEntityConfiguration<Family>
{
    public override void Configure(EntityTypeBuilder<Family> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(x => x.Users)
            .WithOne(x => x.Family)
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Recipes)
            .WithOne(x => x.Family)
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
