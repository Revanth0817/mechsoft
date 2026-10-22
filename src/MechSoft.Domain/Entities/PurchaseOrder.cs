using MechSoft.Domain.Common;

namespace MechSoft.Domain.Entities;

/// <summary>
/// An inbound Purchase Order document (letterhead + line items).
/// </summary>
public class PurchaseOrder
{
    public string Id { get; set; } = string.Empty; // e.g. "PO-24091"

    public string VendorCompany { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string VendorAddress { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;

    public string ShipCompany { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public string ShipAddress { get; set; } = string.Empty;
    public string ShipMobile { get; set; } = string.Empty;

    public DateTime PoDate { get; set; }
    public string QuotationRef { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;
    public string Gstin { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string Signatory { get; set; } = string.Empty;

    /// <summary>Delivery instructions or other notes printed on the order.</summary>
    public string Notes { get; set; } = string.Empty;

    public string CompanyName { get; set; } = LetterheadInfo.CompanyName;
    public string CompanyAddress { get; set; } = LetterheadInfo.CompanyAddress;
    public string CompanyMobile { get; set; } = LetterheadInfo.CompanyMobile;
    public string CompanyEmail { get; set; } = LetterheadInfo.CompanyEmail;
    public string CompanyWebsite { get; set; } = LetterheadInfo.CompanyWebsite;

    // Denormalized summary fields used by seed data / list page when full
    // line items are not reverse-engineered from the original mock data.
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}
