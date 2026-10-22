using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using MechSoft.Domain.Entities;
using MechSoft.Domain.Enums;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class QuoteService : IQuoteService
{
    private readonly ApplicationDbContext _context;

    public QuoteService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<QuoteListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .OrderByDescending(q => q.QuoteDate)
            .Select(q => new QuoteListItemDto(q.Id, q.CustomerCompany, q.QuoteDate, q.ValidUntil, q.Status,
                q.Lines.Count == 0 ? q.TotalValue : q.Lines.Sum(l => l.Amount * (1 + l.GstPercent / 100m)),
                q.ConvertedInvoiceId))
            .ToListAsync(ct);
    }

    public async Task<QuoteDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var q = await _context.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        return q is null ? null : Map(q);
    }

    public async Task<string> SaveAsync(QuoteSaveDto dto, CancellationToken ct = default)
    {
        Quote? q = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            q = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == dto.Id, ct);
        }

        if (q is null)
        {
            q = new Quote { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.Quotes.Add(q);
        }
        else
        {
            _context.QuoteLines.RemoveRange(q.Lines);
            q.Lines.Clear();
        }

        q.CustomerCompany = dto.CustomerCompany;
        q.CustomerName = dto.CustomerName;
        q.CustomerAddress = dto.CustomerAddress;
        q.CustomerEmail = dto.CustomerEmail;
        q.QuoteDate = dto.QuoteDate;
        q.ValidUntil = dto.ValidUntil;
        q.Status = dto.Status;
        q.Gstin = dto.Gstin;
        q.Pan = dto.Pan;
        q.Signatory = dto.Signatory;
        q.Notes = dto.Notes;
        q.CompanyName = dto.CompanyName;
        q.CompanyAddress = dto.CompanyAddress;
        q.CompanyMobile = dto.CompanyMobile;
        q.CompanyEmail = dto.CompanyEmail;
        q.CompanyWebsite = dto.CompanyWebsite;

        foreach (var line in dto.Lines.Where(l => !string.IsNullOrWhiteSpace(l.PartNo) || !string.IsNullOrWhiteSpace(l.Description)))
        {
            q.Lines.Add(new QuoteLine
            {
                QuoteId = q.Id,
                PartNo = line.PartNo,
                Description = line.Description,
                GstPercent = line.GstPercent,
                Qty = line.Qty,
                Unit = line.Unit,
                Rate = line.Rate,
                Amount = line.Amount,
            });
        }

        q.TotalValue = q.Lines.Sum(l => l.Amount * (1 + l.GstPercent / 100m));

        await _context.SaveChangesAsync(ct);
        return q.Id;
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var ids = await _context.Quotes.Select(q => q.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("QT-") && int.TryParse(id.AsSpan(3), out var n) ? n : 0)
            .DefaultIfEmpty(1000)
            .Max() + 1;
        return $"QT-{next}";
    }

    public QuoteDetailDto CreateBlank()
    {
        return new QuoteDetailDto(
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            DateTime.Today, DateTime.Today.AddDays(15), QuoteStatus.Draft, string.Empty, string.Empty, string.Empty, string.Empty,
            LetterheadInfo.CompanyName, LetterheadInfo.CompanyAddress, LetterheadInfo.CompanyMobile,
            LetterheadInfo.CompanyEmail, LetterheadInfo.CompanyWebsite, null,
            new List<QuoteLineDto>());
    }

    public async Task<string> ConvertToInvoiceAsync(string quoteId, CancellationToken ct = default)
    {
        var q = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == quoteId, ct)
            ?? throw new InvalidOperationException($"Quote '{quoteId}' was not found.");

        var ids = await _context.Invoices.Select(i => i.Id).ToListAsync(ct);
        var nextInvoiceNum = ids
            .Select(id => id.StartsWith("INV-") && int.TryParse(id.AsSpan(4), out var n) ? n : 0)
            .DefaultIfEmpty(7700)
            .Max() + 1;
        var invoiceId = $"INV-{nextInvoiceNum}";

        var invoice = new Invoice
        {
            Id = invoiceId,
            CustomerCompany = q.CustomerCompany,
            CustomerName = q.CustomerName,
            CustomerAddress = q.CustomerAddress,
            CustomerEmail = q.CustomerEmail,
            InvoiceDate = DateTime.Today,
            DcRef = string.Empty,
            PaymentTerms = "Net 30",
            Gstin = q.Gstin,
            Pan = q.Pan,
            Signatory = q.Signatory,
            Notes = q.Notes,
            CompanyName = q.CompanyName,
            CompanyAddress = q.CompanyAddress,
            CompanyMobile = q.CompanyMobile,
            CompanyEmail = q.CompanyEmail,
            CompanyWebsite = q.CompanyWebsite,
        };

        foreach (var line in q.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                InvoiceId = invoiceId,
                PartNo = line.PartNo,
                Description = line.Description,
                GstPercent = line.GstPercent,
                Qty = line.Qty,
                Unit = line.Unit,
                Rate = line.Rate,
                Amount = line.Amount,
            });
        }

        invoice.DueDate = invoice.InvoiceDate.AddDays(30);
        invoice.TotalValue = invoice.Lines.Sum(l => l.Total);

        _context.Invoices.Add(invoice);

        q.Status = QuoteStatus.Accepted;
        q.ConvertedInvoiceId = invoiceId;

        await _context.SaveChangesAsync(ct);
        return invoiceId;
    }

    private static QuoteDetailDto Map(Quote q) => new(
        q.Id, q.CustomerCompany, q.CustomerName, q.CustomerAddress, q.CustomerEmail,
        q.QuoteDate, q.ValidUntil, q.Status, q.Gstin, q.Pan, q.Signatory, q.Notes,
        q.CompanyName, q.CompanyAddress, q.CompanyMobile, q.CompanyEmail, q.CompanyWebsite, q.ConvertedInvoiceId,
        q.Lines.Select(l => new QuoteLineDto(l.PartNo, l.Description, l.GstPercent, l.Qty, l.Unit, l.Rate)).ToList());
}
