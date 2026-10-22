using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasMaxLength(20);
        builder.Property(i => i.TotalValue).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Discount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Shipping).HasColumnType("decimal(18,2)");

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.GstPercent).HasColumnType("decimal(5,2)");
        builder.Property(l => l.Qty).HasColumnType("decimal(18,3)");
        builder.Property(l => l.Rate).HasColumnType("decimal(18,4)");
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
        builder.Ignore(l => l.Tax);
        builder.Ignore(l => l.Total);
    }
}
