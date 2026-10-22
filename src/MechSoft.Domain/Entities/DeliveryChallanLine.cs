using MechSoft.Domain.Common;

namespace MechSoft.Domain.Entities;

public class DeliveryChallanLine
{
    public int Id { get; set; }
    public string DcId { get; set; } = string.Empty;
    public DeliveryChallan? DeliveryChallan { get; set; }

    public string PartId { get; set; } = string.Empty;
    public Part? Part { get; set; }

    public decimal GstPercent { get; set; } = LetterheadInfo.DefaultGstPercent;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = LetterheadInfo.DefaultUnit;
    public decimal Rate { get; set; }

    /// <summary>Amount = Qty * Rate. Not stored.</summary>
    public decimal Amount => Qty * Rate;

    /// <summary>Total = Amount * (1 + GstPercent / 100). Not stored.</summary>
    public decimal Total => Amount * (1 + GstPercent / 100m);
}
