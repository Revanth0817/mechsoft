namespace MechSoft.Application.DTOs;

public record PurchaseOrderLineDto(
    string PartNo,
    string Description,
    decimal GstPercent,
    decimal Qty,
    string Unit,
    decimal Rate)
{
    public decimal Amount => Common.GstCalculator.CalculateAmount(Qty, Rate);
    public decimal Total => Common.GstCalculator.CalculateTotal(Amount, GstPercent);
}

public record PurchaseOrderListItemDto(
    string Id,
    string VendorCompany,
    DateTime PoDate,
    int ItemCount,
    decimal TotalValue);

public record PurchaseOrderDetailDto(
    string Id,
    string VendorCompany,
    string VendorName,
    string VendorAddress,
    string VendorEmail,
    string ShipCompany,
    string ShipName,
    string ShipAddress,
    string ShipMobile,
    DateTime PoDate,
    string QuotationRef,
    string ShippingMethod,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public record PurchaseOrderSaveDto(
    string? Id,
    string VendorCompany,
    string VendorName,
    string VendorAddress,
    string VendorEmail,
    string ShipCompany,
    string ShipName,
    string ShipAddress,
    string ShipMobile,
    DateTime PoDate,
    string QuotationRef,
    string ShippingMethod,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<PurchaseOrderLineDto> Lines);
