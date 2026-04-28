using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Mappings;

public class GlobalConfigConfiguration : IEntityTypeConfiguration<GlobalConfig>
{
    public void Configure(EntityTypeBuilder<GlobalConfig> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Key).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Value).HasMaxLength(4000).IsRequired();
    }
}
