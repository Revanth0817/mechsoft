using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("Parts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(20);
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Material).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.Photo).HasColumnType("varbinary(max)");
        builder.Property(p => p.PhotoContentType).HasMaxLength(100);
        builder.Property(p => p.VendorName).HasMaxLength(200);
        builder.Property(p => p.VendorContactPerson).HasMaxLength(100);
        builder.Property(p => p.VendorEmail).HasMaxLength(200);
        builder.Property(p => p.VendorPhone).HasMaxLength(30);

        builder.HasMany(p => p.Specs)
            .WithOne(s => s.Part)
            .HasForeignKey(s => s.PartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Sku);
    }
}

public class PartSpecConfiguration : IEntityTypeConfiguration<PartSpec>
{
    public void Configure(EntityTypeBuilder<PartSpec> builder)
    {
        builder.ToTable("PartSpecs");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(200).IsRequired();
    }
}
