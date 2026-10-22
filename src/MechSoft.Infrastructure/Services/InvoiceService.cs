using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using MechSoft.Domain.Entities;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;

    public InvoiceService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<InvoiceListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceListItemDto(i.Id, i.CustomerCompany, i.InvoiceDate,
                i.Lines.Count == 0
                    ? i.TotalValue
                    : i.Lines.Sum(l => l.Amount * (1 + l.GstPercent / 100m)) - i.Discount + i.Shipping))
            .ToListAsync(ct);
    }

    public async Task<InvoiceDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var inv = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        return inv is null ? null : Map(inv);
    }

    public async Task<string> SaveAsync(InvoiceSaveDto dto, CancellationToken ct = default)
    {
        Invoice? inv = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            inv = await _context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == dto.Id, ct);
        }

        if (inv is null)
        {
            inv = new Invoice { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.Invoices.Add(inv);
        }
        else
        {
            _context.InvoiceLines.RemoveRange(inv.Lines);
            inv.Lines.Clear();
        }

        inv.CustomerCompany = dto.CustomerCompany;
        inv.CustomerName = dto.CustomerName;
        inv.CustomerAddress = dto.CustomerAddress;
        inv.CustomerEmail = dto.CustomerEmail;
        inv.InvoiceDate = dto.InvoiceDate;
        inv.DueDate = dto.DueDate;
        inv.DcRef = dto.DcRef;
        inv.PaymentTerms = dto.PaymentTerms;
        inv.Notes = dto.Notes;
        inv.Discount = dto.Discount;
        inv.Shipping = dto.Shipping;
        inv.Gstin = dto.Gstin;
        inv.Pan = dto.Pan;
        inv.Signatory = dto.Signatory;
        inv.CompanyName = dto.CompanyName;
        inv.CompanyAddress = dto.CompanyAddress;
        inv.CompanyMobile = dto.CompanyMobile;
        inv.CompanyEmail = dto.CompanyEmail;
        inv.CompanyWebsite = dto.CompanyWebsite;

        foreach (var line in dto.Lines.Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description)))
        {
            inv.Lines.Add(new InvoiceLine
            {
                InvoiceId = inv.Id,
                PartNo = line.PartNo,
                Description = line.Description,
                GstPercent = line.GstPercent,
                Qty = line.Qty,
                Unit = line.Unit,
                Rate = line.Rate,
                Amount = line.Amount,
            });
        }

        inv.TotalValue = inv.Lines.Sum(l => l.Total) - inv.Discount + inv.Shipping;

        await _context.SaveChangesAsync(ct);
        return inv.Id;
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var ids = await _context.Invoices.Select(i => i.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("INV-") && int.TryParse(id.AsSpan(4), out var n) ? n : 0)
            .DefaultIfEmpty(7700)
            .Max() + 1;
        return $"INV-{next}";
    }

    public InvoiceDetailDto CreateBlank()
    {
        return new InvoiceDetailDto(
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            DateTime.Today, DateTime.Today.AddDays(30), string.Empty, "Net 30", string.Empty, 0m, 0m,
            string.Empty, string.Empty, string.Empty,
            LetterheadInfo.CompanyName, LetterheadInfo.CompanyAddress, LetterheadInfo.CompanyMobile,
            LetterheadInfo.CompanyEmail, LetterheadInfo.CompanyWebsite,
            new List<InvoiceLineDto>());
    }

    private static InvoiceDetailDto Map(Invoice inv) => new(
        inv.Id, inv.CustomerCompany, inv.CustomerName, inv.CustomerAddress, inv.CustomerEmail,
        inv.InvoiceDate, inv.DueDate, inv.DcRef, inv.PaymentTerms, inv.Notes, inv.Discount, inv.Shipping,
        inv.Gstin, inv.Pan, inv.Signatory,
        inv.CompanyName, inv.CompanyAddress, inv.CompanyMobile, inv.CompanyEmail, inv.CompanyWebsite,
        inv.Lines.Select(l => new InvoiceLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate)).ToList());
}
