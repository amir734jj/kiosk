using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Mappings;

public class KioskAgentInstanceConfiguration : IEntityTypeConfiguration<KioskAgentInstance>
{
    public void Configure(EntityTypeBuilder<KioskAgentInstance> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.MachineName).IsUnique();
        builder.Property(a => a.MachineName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.AgentVersion).HasMaxLength(50);
        builder.Property(a => a.DisplayUrl).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
    }
}
