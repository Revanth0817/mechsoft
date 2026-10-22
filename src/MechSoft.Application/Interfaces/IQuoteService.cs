using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IQuoteService
{
    Task<IReadOnlyList<QuoteListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<QuoteDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<string> SaveAsync(QuoteSaveDto dto, CancellationToken ct = default);
    Task<string> GenerateNextIdAsync(CancellationToken ct = default);
    QuoteDetailDto CreateBlank();

    /// <summary>
    /// Creates a new Invoice pre-filled from this quote's customer details and line
    /// items, marks the quote Accepted, and records the resulting invoice id on it.
    /// Returns the new invoice's id.
    /// </summary>
    Task<string> ConvertToInvoiceAsync(string quoteId, CancellationToken ct = default);
}
