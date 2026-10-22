using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class CadFileConfiguration : IEntityTypeConfiguration<CadFile>
{
    public void Configure(EntityTypeBuilder<CadFile> builder)
    {
        builder.ToTable("CadFiles");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.PartId).HasMaxLength(20);
        builder.Property(f => f.FileName).HasMaxLength(260).IsRequired();
        builder.Property(f => f.Extension).HasMaxLength(10).IsRequired();
        builder.Property(f => f.ApsUrn).HasMaxLength(600);
        builder.Property(f => f.ApsStatus).HasMaxLength(20);
        builder.Property(f => f.Content).HasColumnType("varbinary(max)");

        builder.HasOne(f => f.Part)
            .WithMany(p => p.CadFiles)
            .HasForeignKey(f => f.PartId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.PartId);
    }
}
