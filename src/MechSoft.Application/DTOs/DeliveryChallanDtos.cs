namespace MechSoft.Application.DTOs;

public record DeliveryChallanLineDto(
    string PartId,
    string PartLabel, // "SKU - Name", populated for display
    int AvailableStock,
    decimal GstPercent,
    decimal Qty,
    string Unit,
    decimal Rate)
{
    public decimal Amount => Common.GstCalculator.CalculateAmount(Qty, Rate);
    public decimal Total => Common.GstCalculator.CalculateTotal(Amount, GstPercent);
    public bool IsOverage => Qty > AvailableStock;
}

public record DeliveryChallanListItemDto(
    string Id,
    string CustomerCompany,
    DateTime DcDate,
    int LineItemCount,
    decimal TotalValue);

public record DeliveryChallanDetailDto(
    string Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerMobile,
    DateTime DcDate,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<DeliveryChallanLineDto> Lines)
{
    public decimal Subtotal => Lines.Sum(l => l.Amount);
    public decimal Total => Lines.Sum(l => l.Total);
    public decimal Taxes => Total - Subtotal;
}

public record DeliveryChallanSaveDto(
    string? Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerMobile,
    DateTime DcDate,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<DeliveryChallanLineDto> Lines);

public record DeliveryChallanSaveResult(bool Success, IReadOnlyList<string> Errors, string? Id);
