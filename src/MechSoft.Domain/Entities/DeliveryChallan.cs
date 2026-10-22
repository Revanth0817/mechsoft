using MechSoft.Domain.Common;

namespace MechSoft.Domain.Entities;

/// <summary>
/// An outbound Delivery Challan document (letterhead + line items).
/// </summary>
public class DeliveryChallan
{
    public string Id { get; set; } = string.Empty; // e.g. "DC-11029"

    public string CustomerCompany { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;

    public DateTime DcDate { get; set; }
    public string Gstin { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string Signatory { get; set; } = string.Empty;

    /// <summary>Transport details or other remarks printed on the challan.</summary>
    public string Notes { get; set; } = string.Empty;

    public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
    public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
    public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
    public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
    public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;

    public List<DeliveryChallanLine> Lines { get; set; } = new();
}
