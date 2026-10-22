namespace MechSoft.Application.Interfaces;

public interface IPdfDocumentService
{
    Task<byte[]?> GeneratePurchaseOrderPdfAsync(string id, CancellationToken ct = default);
    Task<byte[]?> GenerateDeliveryChallanPdfAsync(string id, CancellationToken ct = default);
    Task<byte[]?> GenerateInvoicePdfAsync(string id, CancellationToken ct = default);
    Task<byte[]?> GenerateQuotePdfAsync(string id, CancellationToken ct = default);
}
