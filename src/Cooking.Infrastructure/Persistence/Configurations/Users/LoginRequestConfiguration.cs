using Cooking.Domain.Entities.Users;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Users;

public class LoginRequestConfiguration : BaseEntityConfiguration<LoginRequest>
{
    public override void Configure(EntityTypeBuilder<LoginRequest> builder)
    {
        base.Configure(builder);

        // Code идёт в start-параметр deep link'а: ≤ 64 символов [A-Za-z0-9_-] вместе с префиксом "login_".
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();

        // SHA-256 в hex.
        builder.Property(x => x.PollTokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.PollTokenHash).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
