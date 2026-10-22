using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IDeliveryChallanService
{
    Task<IReadOnlyList<DeliveryChallanListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<DeliveryChallanDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<DeliveryChallanSaveResult> SaveAsync(DeliveryChallanSaveDto dto, CancellationToken ct = default);
    Task<string> GenerateNextIdAsync(CancellationToken ct = default);
    Task<DeliveryChallanDetailDto> CreateBlankAsync(CancellationToken ct = default);

    /// <summary>Validates line items against current Part stock (Qty must not exceed available stock).</summary>
    IReadOnlyList<string> ValidateStock(IReadOnlyList<DeliveryChallanLineDto> lines);
}
