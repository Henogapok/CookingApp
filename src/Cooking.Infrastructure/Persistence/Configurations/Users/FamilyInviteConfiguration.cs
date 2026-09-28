using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Users;

public class FamilyInviteConfiguration : BaseEntityConfiguration<FamilyInvite>
{
    public override void Configure(EntityTypeBuilder<FamilyInvite> builder)
    {
        base.Configure(builder);

        // Telegram ограничивает start-параметр deep link'а 64 символами [A-Za-z0-9_-].
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
