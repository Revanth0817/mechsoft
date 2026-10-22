using MechSoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechSoft.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasMaxLength(20);
        builder.Property(q => q.TotalValue).HasColumnType("decimal(18,2)");
        builder.Property(q => q.ConvertedInvoiceId).HasMaxLength(20);

        builder.HasMany(q => q.Lines)
            .WithOne(l => l.Quote)
            .HasForeignKey(l => l.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.ToTable("QuoteLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.GstPercent).HasColumnType("decimal(5,2)");
        builder.Property(l => l.Qty).HasColumnType("decimal(18,3)");
        builder.Property(l => l.Rate).HasColumnType("decimal(18,4)");
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
        builder.Ignore(l => l.Total);
    }
}
