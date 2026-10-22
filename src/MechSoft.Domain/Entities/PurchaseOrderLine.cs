using MechSoft.Domain.Common;

namespace MechSoft.Domain.Entities;

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public string PoId { get; set; } = string.Empty;
    public PurchaseOrder? PurchaseOrder { get; set; }

    public string PartNo { get; set; } = string.Empty; // free text, not FK
    public string Description { get; set; } = string.Empty;
    public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
    public decimal Qty { get; set; } = 1;
    public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
    public decimal Rate { get; set; }

    /// <summary>Amount = Qty * Rate (before tax). Stored so totals can be queried in SQL.</summary>
    public decimal Amount { get; set; }

    /// <summary>Total = Amount * (1 + GstPercent / 100). Not stored.</summary>
    public decimal Total => Amount * (1 + GstPercent / 100m);
}
