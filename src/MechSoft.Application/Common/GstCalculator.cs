namespace MechSoft.Application.Common;

/// <summary>
/// Centralizes the GST/total calculation formula so it is defined exactly
/// once and is unit-testable, instead of being duplicated across pages.
/// </summary>
public static class GstCalculator
{
    /// <summary>total = amount * (1 + gstPercent / 100)</summary>
    public static decimal CalculateTotal(decimal amount, decimal gstPercent)
        => amount * (1 + gstPercent / 100m);

    /// <summary>amount = qty * rate</summary>
    public static decimal CalculateAmount(decimal qty, decimal rate) => qty * rate;
}
