namespace MechSoft.Application.DTOs;

/// <summary>One invoice line: Amount = Qty x Rate, Tax = Amount x GST%, Total = Amount + Tax.</summary>
public record InvoiceLineDto(
    string PartNo,
    string Description,
    decimal GstPercent,
    decimal Qty,
    string Unit,
    decimal Rate)
{
    public decimal Amount => Common.GstCalculator.CalculateAmount(Qty, Rate);
    public decimal Tax => Amount * GstPercent / 100m;
    public decimal Total => Amount + Tax;
}

public record InvoiceListItemDto(
    string Id,
    string CustomerCompany,
    DateTime InvoiceDate,
    decimal TotalValue);

public record InvoiceDetailDto(
    string Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerEmail,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string DcRef,
    string PaymentTerms,
    string Notes,
    decimal Discount,
    decimal Shipping,
    string Gstin,
    string Pan,
    string Signatory,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<InvoiceLineDto> Lines)
{
    public decimal Subtotal => Lines.Sum(l => l.Amount);
    public decimal Taxes => Lines.Sum(l => l.Tax);
    public decimal BalanceDue => Subtotal + Taxes - Discount + Shipping;
}

public record InvoiceSaveDto(
    string? Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerEmail,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string DcRef,
    string PaymentTerms,
    string Notes,
    decimal Discount,
    decimal Shipping,
    string Gstin,
    string Pan,
    string Signatory,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<InvoiceLineDto> Lines);
