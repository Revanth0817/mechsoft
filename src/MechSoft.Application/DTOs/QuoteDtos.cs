using MechSoft.Domain.Enums;

namespace MechSoft.Application.DTOs;

public record QuoteLineDto(
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

public record QuoteListItemDto(
    string Id,
    string CustomerCompany,
    DateTime QuoteDate,
    DateTime? ValidUntil,
    QuoteStatus Status,
    decimal TotalValue,
    string? ConvertedInvoiceId);

public record QuoteDetailDto(
    string Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerEmail,
    DateTime QuoteDate,
    DateTime? ValidUntil,
    QuoteStatus Status,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    string? ConvertedInvoiceId,
    IReadOnlyList<QuoteLineDto> Lines);

public record QuoteSaveDto(
    string? Id,
    string CustomerCompany,
    string CustomerName,
    string CustomerAddress,
    string CustomerEmail,
    DateTime QuoteDate,
    DateTime? ValidUntil,
    QuoteStatus Status,
    string Gstin,
    string Pan,
    string Signatory,
    string Notes,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<QuoteLineDto> Lines);
