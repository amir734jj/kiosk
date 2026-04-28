using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Api.Data.Mappings;

public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.UnitNumber).HasMaxLength(50).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Names)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<string>>(v) ?? new List<string>())
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (a, b) => JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b),
                    v => JsonConvert.SerializeObject(v).GetHashCode(),
                    v => v.ToList()));
        builder.Property(o => o.PhoneNumber).HasMaxLength(50);
        builder.Property(o => o.Note).HasMaxLength(500);
    }
}
