using Cooking.Domain.Entities.Ingredients;
using Cooking.Infrastructure.Persistence.Configurations.Common;
using Cooking.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cooking.Infrastructure.Persistence.Configurations.Ingredients;

public class DataSourceConfiguration : ReferenceEntityConfiguration<DataSource>
{
    public override void Configure(EntityTypeBuilder<DataSource> builder)
    {
        base.Configure(builder);

        builder.HasData(ReferenceDataSeed.DataSources);
    }
}
