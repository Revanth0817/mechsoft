using MechSoft.Domain.Common;

namespace MechSoft.Domain.Entities;

/// <summary>
/// A Tax Invoice document (letterhead + line items).
/// </summary>
public class Invoice
{
    public string Id { get; set; } = string.Empty; // e.g. "INV-7701"

    public string CustomerCompany { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    /// <summary>Payment instructions or other notes printed on the invoice.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Flat amount taken off Subtotal + Taxes.</summary>
    public decimal Discount { get; set; }

    /// <summary>Flat amount added on top of Subtotal + Taxes.</summary>
    public decimal Shipping { get; set; }

    public string DcRef { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string Gstin { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string Signatory { get; set; } = string.Empty;

    public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
    public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
    public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
    public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
    public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;

    // Denormalized total used by seed rows that don't reverse-engineer full
    // line items from the original mock data.
    public decimal TotalValue { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();
}
