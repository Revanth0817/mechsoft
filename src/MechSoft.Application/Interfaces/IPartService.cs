using MechSoft.Application.DTOs;
using MechSoft.Domain.Enums;

namespace MechSoft.Application.Interfaces;

public interface IPartService
{
    /// <summary>
    /// Trimmed, case-insensitive, literal substring match on SKU, name, material and vendor name
    /// (REQ-06). %, _, [, ] and \ have no special meaning. Category, contact, email, phone and notes
    /// are not searched. A term that is empty after trimming returns every part.
    /// </summary>
    Task<IReadOnlyList<PartListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default);

    Task<PartDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates a part from its editable fields only. The server assigns the Id (next P-number),
    /// Stock 0, Status Out of Stock and Version 1 (REQ-04). Every text field is trimmed before
    /// validation and storage (REQ-07).
    /// </summary>
    Task<PartSaveResult> CreateAsync(PartSaveDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates a part's editable fields only - stock and status are never written here (REQ-04).
    /// Rejects the update (<see cref="PartSaveOutcome.Conflict"/>) if <paramref name="version"/> does
    /// not match the stored version (REQ-02). Every text field is trimmed before validation and
    /// storage (REQ-07).
    /// </summary>
    Task<PartSaveResult> UpdateAsync(string id, int version, PartSaveDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates only the stock quantity and status of a part (used by the Inventory page). Rejects the
    /// update (<see cref="StockUpdateOutcome.Conflict"/>) if <paramref name="version"/> does not match
    /// the stored version (REQ-02), returning the part's current stock, status and version either way.
    /// </summary>
    Task<StockUpdateResult> UpdateStockAsync(string id, int version, int stock, StockStatus status, CancellationToken ct = default);

    Task<string> GenerateNextIdAsync(CancellationToken ct = default);

    /// <summary>Saves a product photo, replacing any existing one. Returns false if the part does not exist. Does not change Version.</summary>
    Task<bool> UpdatePhotoAsync(string id, string contentType, byte[] content, CancellationToken ct = default);

    Task<PhotoContentDto?> GetPhotoAsync(string id, CancellationToken ct = default);

    /// <summary>Does not change Version.</summary>
    Task<bool> RemovePhotoAsync(string id, CancellationToken ct = default);
}
