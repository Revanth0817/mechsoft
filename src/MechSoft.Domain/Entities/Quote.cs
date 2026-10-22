using MechSoft.Domain.Common;
using MechSoft.Domain.Enums;

namespace MechSoft.Domain.Entities;

/// <summary>
/// A sales quotation prepared for a customer, ahead of a Delivery Challan / Invoice.
/// </summary>
public class Quote
{
    public string Id { get; set; } = string.Empty; // e.g. "QT-1001"

    public string CustomerCompany { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public string Gstin { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string Signatory { get; set; } = string.Empty;

    /// <summary>Terms or other notes printed on the quotation.</summary>
    public string Notes { get; set; } = string.Empty;

    public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
    public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
    public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
    public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
    public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;

    /// <summary>Id of the Invoice this quote was converted into, if any.</summary>
    public string? ConvertedInvoiceId { get; set; }

    public decimal TotalValue { get; set; }

    public List<QuoteLine> Lines { get; set; } = new();
}
