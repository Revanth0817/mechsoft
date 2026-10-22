using MechSoft.Domain.Enums;

namespace MechSoft.Application.DTOs;

public record PartSpecDto(string Label, string Value, int SortOrder);

public record PartListItemDto(
    string Id,
    string Sku,
    string Name,
    string Material,
    string Category,
    int Stock,
    StockStatus Status,
    bool HasPhoto);

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
    bool HasPhoto);

public record PartSaveDto(
    string? Id,
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
    string VendorPhone);
