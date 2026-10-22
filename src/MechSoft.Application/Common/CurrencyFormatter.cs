using System.Globalization;

namespace MechSoft.Application.Common;

/// <summary>
/// Formats amounts as Indian Rupees with en-IN digit grouping, e.g. "₹ 1,23,456.78".
/// </summary>
public static class CurrencyFormatter
{
    private static readonly CultureInfo EnIn = CultureInfo.GetCultureInfo("en-IN");

    public static string Format(decimal amount)
        => "₹ " + amount.ToString("N2", EnIn);
}
