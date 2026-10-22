using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<InvoiceDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<string> SaveAsync(InvoiceSaveDto dto, CancellationToken ct = default);
    Task<string> GenerateNextIdAsync(CancellationToken ct = default);
    InvoiceDetailDto CreateBlank();
}
