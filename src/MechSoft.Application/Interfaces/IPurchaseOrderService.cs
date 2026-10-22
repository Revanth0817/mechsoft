using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseOrderDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<string> SaveAsync(PurchaseOrderSaveDto dto, CancellationToken ct = default);
    Task<string> GenerateNextIdAsync(CancellationToken ct = default);
    PurchaseOrderDetailDto CreateBlank();
}
