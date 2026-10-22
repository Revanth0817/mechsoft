using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(20);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.MachineType).HasMaxLength(150).IsRequired();
        builder.Property(p => p.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.Photo).HasColumnType("varbinary(max)");
        builder.Property(p => p.PhotoContentType).HasMaxLength(100);
    }
}
