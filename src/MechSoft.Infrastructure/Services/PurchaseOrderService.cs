using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using MechSoft.Domain.Entities;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PurchaseOrderListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(p => p.PoDate)
            .Select(p => new PurchaseOrderListItemDto(p.Id, p.VendorCompany, p.PoDate,
                p.Lines.Count == 0 ? p.ItemCount : p.Lines.Count,
                p.Lines.Count == 0 ? p.TotalValue : p.Lines.Sum(l => l.Amount * (1 + l.GstPercent / 100m))))
            .ToListAsync(ct);
    }

    public async Task<PurchaseOrderDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null) return null;

        return Map(po);
    }

    public async Task<string> SaveAsync(PurchaseOrderSaveDto dto, CancellationToken ct = default)
    {
        PurchaseOrder? po = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            po = await _context.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        }

        if (po is null)
        {
            po = new PurchaseOrder { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.PurchaseOrders.Add(po);
        }
        else
        {
            _context.PurchaseOrderLines.RemoveRange(po.Lines);
            po.Lines.Clear();
        }

        po.VendorCompany = dto.VendorCompany;
        po.VendorName = dto.VendorName;
        po.VendorAddress = dto.VendorAddress;
        po.VendorEmail = dto.VendorEmail;
        po.ShipCompany = dto.ShipCompany;
        po.ShipName = dto.ShipName;
        po.ShipAddress = dto.ShipAddress;
        po.ShipMobile = dto.ShipMobile;
        po.PoDate = dto.PoDate;
        po.QuotationRef = dto.QuotationRef;
        po.ShippingMethod = dto.ShippingMethod;
        po.Gstin = dto.Gstin;
        po.Pan = dto.Pan;
        po.Signatory = dto.Signatory;
        po.Notes = dto.Notes;
        po.CompanyName = dto.CompanyName;
        po.CompanyAddress = dto.CompanyAddress;
        po.CompanyMobile = dto.CompanyMobile;
        po.CompanyEmail = dto.CompanyEmail;
        po.CompanyWebsite = dto.CompanyWebsite;

        foreach (var line in dto.Lines.Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description)))
        {
            po.Lines.Add(new PurchaseOrderLine
            {
                PoId = po.Id,
                PartNo = line.PartNo,
                Description = line.Description,
                GstPercent = line.GstPercent,
                Qty = line.Qty,
                Unit = line.Unit,
                Rate = line.Rate,
                Amount = line.Amount,
            });
        }

        po.ItemCount = po.Lines.Count;
        po.TotalValue = po.Lines.Sum(l => l.Amount * (1 + l.GstPercent / 100m));

        await _context.SaveChangesAsync(ct);
        return po.Id;
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var ids = await _context.PurchaseOrders.Select(p => p.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("PO-") && int.TryParse(id.AsSpan(3), out var n) ? n : 0)
            .DefaultIfEmpty(24090)
            .Max() + 1;
        return $"PO-{next}";
    }

    public PurchaseOrderDetailDto CreateBlank()
    {
        return new PurchaseOrderDetailDto(
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, string.Empty,
            DateTime.Today, string.Empty, "Road", string.Empty, string.Empty, string.Empty, string.Empty,
            LetterheadInfo.CompanyName, LetterheadInfo.CompanyAddress, LetterheadInfo.CompanyMobile,
            LetterheadInfo.CompanyEmail, LetterheadInfo.CompanyWebsite,
            new List<PurchaseOrderLineDto>());
    }

    private static PurchaseOrderDetailDto Map(PurchaseOrder po) => new(
        po.Id, po.VendorCompany, po.VendorName, po.VendorAddress, po.VendorEmail,
        po.ShipCompany, po.ShipName, po.ShipAddress, po.ShipMobile,
        po.PoDate, po.QuotationRef, po.ShippingMethod, po.Gstin, po.Pan, po.Signatory, po.Notes,
        po.CompanyName, po.CompanyAddress, po.CompanyMobile, po.CompanyEmail, po.CompanyWebsite,
        po.Lines.Select(l => new PurchaseOrderLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate)).ToList());
}
