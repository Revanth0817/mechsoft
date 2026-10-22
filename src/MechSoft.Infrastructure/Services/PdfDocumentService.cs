using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Infrastructure.Pdf;

namespace MechSoft.Infrastructure.Services;

public class PdfDocumentService : IPdfDocumentService
{
    private readonly IPurchaseOrderService _purchaseOrders;
    private readonly IDeliveryChallanService _deliveryChallans;
    private readonly IInvoiceService _invoices;
    private readonly IQuoteService _quotes;

    public PdfDocumentService(
        IPurchaseOrderService purchaseOrders,
        IDeliveryChallanService deliveryChallans,
        IInvoiceService invoices,
        IQuoteService quotes)
    {
        _purchaseOrders = purchaseOrders;
        _deliveryChallans = deliveryChallans;
        _invoices = invoices;
        _quotes = quotes;
    }

    public async Task<byte[]?> GeneratePurchaseOrderPdfAsync(string id, CancellationToken ct = default)
    {
        var po = await _purchaseOrders.GetByIdAsync(id, ct);
        return po is null ? null : ServiceDocumentPdfBuilder.FromPurchaseOrder(po);
    }

    public async Task<byte[]?> GenerateDeliveryChallanPdfAsync(string id, CancellationToken ct = default)
    {
        var dc = await _deliveryChallans.GetByIdAsync(id, ct);
        return dc is null ? null : ServiceDocumentPdfBuilder.FromDeliveryChallan(dc);
    }

    public async Task<byte[]?> GenerateInvoicePdfAsync(string id, CancellationToken ct = default)
    {
        var inv = await _invoices.GetByIdAsync(id, ct);
        return inv is null ? null : ServiceDocumentPdfBuilder.FromInvoice(inv);
    }

    public async Task<byte[]?> GenerateQuotePdfAsync(string id, CancellationToken ct = default)
    {
        var q = await _quotes.GetByIdAsync(id, ct);
        return q is null ? null : ServiceDocumentPdfBuilder.FromQuote(q);
    }

}
