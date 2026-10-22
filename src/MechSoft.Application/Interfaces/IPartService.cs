using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IPartService
{
    Task<IReadOnlyList<PartListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<PartDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<string> SaveAsync(PartSaveDto dto, CancellationToken ct = default);
    /// <summary>Updates only the stock quantity and status of a part (used by the Inventory page).</summary>
    Task<bool> UpdateStockAsync(string id, int stock, MechSoft.Domain.Enums.StockStatus status, CancellationToken ct = default);

    Task<string> GenerateNextIdAsync(CancellationToken ct = default);

    /// <summary>Saves a product photo, replacing any existing one. Returns false if the part does not exist.</summary>
    Task<bool> UpdatePhotoAsync(string id, string contentType, byte[] content, CancellationToken ct = default);

    Task<PhotoContentDto?> GetPhotoAsync(string id, CancellationToken ct = default);

    Task<bool> RemovePhotoAsync(string id, CancellationToken ct = default);
}
