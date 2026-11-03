using MechSoft.Domain.Enums;

namespace MechSoft.Domain.Entities;

/// <summary>
/// A manufacturing component in the Product Master / eDrawings catalog.
/// </summary>
public class Part
{
    public string Id { get; set; } = string.Empty; // e.g. "P-001"
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }

    // Status is stored independently, not derived from Stock - matches the
    // original React mock data where status is set at creation time.
    public StockStatus Status { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Starts at 1 and increases by 1 on every successful Inventory
    /// stock/status update and every successful Product Details edit. Photo changes do not touch it.
    /// </summary>
    public int Version { get; set; } = 1;

    // Product photo, stored in the database so it is shared by everyone using the app.
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }

    // Vendor / supplier details for this product.
    public string VendorName { get; set; } = string.Empty;
    public string VendorContactPerson { get; set; } = string.Empty;
    public string VendorEmail { get; set; } = string.Empty;
    public string VendorPhone { get; set; } = string.Empty;

    public List<CadFile> CadFiles { get; set; } = new();
    public List<PartSpec> Specs { get; set; } = new();
    public List<DeliveryChallanLine> DeliveryChallanLines { get; set; } = new();
}
