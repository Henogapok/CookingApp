using Cooking.Domain.Entities.Recipes;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Recipes;

public class RecipeDraftConfiguration : BaseEntityConfiguration<RecipeDraft>
{
    public override void Configure(EntityTypeBuilder<RecipeDraft> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SourceText).IsRequired();
        builder.Property(x => x.ContentJson).HasColumnType("jsonb");
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);
        builder.Property(x => x.MediaFilePath).HasMaxLength(1024);
        builder.Property(x => x.IsSourceLoaded).HasDefaultValue(true);

        // Для удаления просроченных черновиков.
        builder.HasIndex(x => x.ExpiresAt);

        // Черновики — временные данные: удалился пользователь — удаляются и они.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
