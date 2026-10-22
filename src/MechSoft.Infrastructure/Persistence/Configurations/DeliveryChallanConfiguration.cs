using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class DeliveryChallanConfiguration : IEntityTypeConfiguration<DeliveryChallan>
{
    public void Configure(EntityTypeBuilder<DeliveryChallan> builder)
    {
        builder.ToTable("DeliveryChallans");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasMaxLength(20);

        builder.HasMany(d => d.Lines)
            .WithOne(l => l.DeliveryChallan)
            .HasForeignKey(l => l.DcId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeliveryChallanLineConfiguration : IEntityTypeConfiguration<DeliveryChallanLine>
{
    public void Configure(EntityTypeBuilder<DeliveryChallanLine> builder)
    {
        builder.ToTable("DeliveryChallanLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.GstPercent).HasColumnType("decimal(5,2)");
        builder.Property(l => l.Qty).HasColumnType("decimal(18,3)");
        builder.Property(l => l.Rate).HasColumnType("decimal(18,2)");
        builder.Ignore(l => l.Amount);
        builder.Ignore(l => l.Total);

        builder.HasOne(l => l.Part)
            .WithMany(p => p.DeliveryChallanLines)
            .HasForeignKey(l => l.PartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
