using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(20);
        builder.Property(p => p.TotalValue).HasColumnType("decimal(18,2)");

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.GstPercent).HasColumnType("decimal(5,2)");
        builder.Property(l => l.Qty).HasColumnType("decimal(18,3)");
        builder.Property(l => l.Rate).HasColumnType("decimal(18,4)");
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
        builder.Ignore(l => l.Total);
    }
}
