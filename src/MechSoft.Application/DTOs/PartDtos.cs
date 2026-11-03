using MechSoft.Domain.Enums;

namespace MechSoft.Application.DTOs;

public record PartSpecDto(string Label, string Value, int SortOrder);

public record PartListItemDto(
    string Id,
    string Sku,
    string Name,
    string Material,
    string Category,
    string VendorName,
    int Stock,
    StockStatus Status,
    bool HasPhoto,
    int Version);

public record PartDetailDto(
    string Id,
    string Sku,
    string Name,
    string Material,
    string Category,
    int Stock,
    StockStatus Status,
    string? Notes,
    IReadOnlyList<PartSpecDto> Specs,
    string VendorName,
    string VendorContactPerson,
    string VendorEmail,
    string VendorPhone,
    bool HasPhoto,
    int Version);

/// <summary>
/// The editable fields of a part. No Id, Stock, Status or Version (REQ-04): identity comes from the
/// URL on update and is server-assigned on create; stock/status are only ever changed from Inventory.
/// </summary>
public record PartSaveDto(
    string Sku,
    string Name,
    string Material,
    string Category,
    string? Notes,
    IReadOnlyList<PartSpecDto> Specs,
    string VendorName,
    string VendorContactPerson,
    string VendorEmail,
    string VendorPhone);

public record PartFieldError(string Field, string Message);

public enum PartSaveOutcome
{
    Success,
    ValidationFailed,
    NotFound,
    Conflict,
}

public record PartSaveResult(PartSaveOutcome Outcome, string? Id, IReadOnlyList<PartFieldError> Errors)
{
    public static PartSaveResult Success(string id) => new(PartSaveOutcome.Success, id, Array.Empty<PartFieldError>());
    public static PartSaveResult Invalid(IReadOnlyList<PartFieldError> errors) => new(PartSaveOutcome.ValidationFailed, null, errors);
    public static readonly PartSaveResult NotFoundResult = new(PartSaveOutcome.NotFound, null, Array.Empty<PartFieldError>());
    public static readonly PartSaveResult ConflictResult = new(PartSaveOutcome.Conflict, null, Array.Empty<PartFieldError>());
}

public enum StockUpdateOutcome
{
    Success,
    NotFound,
    Conflict,
}

public record StockUpdateResult(StockUpdateOutcome Outcome, int Stock, StockStatus Status, int Version);
