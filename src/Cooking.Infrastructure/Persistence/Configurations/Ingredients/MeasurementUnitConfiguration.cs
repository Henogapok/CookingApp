using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Ingredients;

public class MeasurementUnitConfiguration : ReferenceEntityConfiguration<MeasurementUnit>
{
    public override void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Abbreviation)
            .IsRequired()
            .HasMaxLength(20);
    }
}
