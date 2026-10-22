namespace MechSoft.Application.DTOs;

/// <summary>A label/value row, e.g. "PO No" / "PO-24092" or "GST No" / "33AAC...".</summary>
public record PdfMetaRow(string Label, string Value);

/// <summary>A vendor/shipping/customer address block, e.g. "Vendor Address" or "Ship To".</summary>
public record PdfPartyBlock(string Title, string Company, string Name, string Address, string Contact);

public record PdfLineRow(
    string PartNo,
    string Description,
    decimal GstPercent,
    decimal Qty,
    string Unit,
    decimal? Rate,
    decimal Amount,
    decimal Total);

/// <summary>
/// A print-ready shape shared by every PDF document type (Purchase Order, Delivery
/// Challan, Invoice, Quote) so one QuestPDF layout can render all four.
/// </summary>
public record PdfDocumentDto(
    string DocumentTypeLabel,
    string Id,
    string CompanyName,
    string CompanyAddress,
    string CompanyMobile,
    string CompanyEmail,
    string CompanyWebsite,
    IReadOnlyList<PdfMetaRow> MetaRows,
    IReadOnlyList<PdfPartyBlock> PartyBlocks,
    IReadOnlyList<PdfMetaRow> ExtraFields,
    bool ShowRateColumn,
    bool ShowPartNoColumn,
    IReadOnlyList<PdfLineRow> Lines,
    decimal GrandTotal,
    string Gstin,
    string Pan,
    string Signatory);
