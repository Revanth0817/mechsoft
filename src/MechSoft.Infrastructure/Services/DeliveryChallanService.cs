using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Common;
using MechSoft.Domain.Entities;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class DeliveryChallanService : IDeliveryChallanService
{
    private readonly ApplicationDbContext _context;

    public DeliveryChallanService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeliveryChallanListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.DeliveryChallans
            .AsNoTracking()
            .Include(d => d.Lines)
            .OrderByDescending(d => d.DcDate)
            .Select(d => new DeliveryChallanListItemDto(d.Id, d.CustomerCompany, d.DcDate, d.Lines.Count,
                d.Lines.Sum(l => l.Qty * l.Rate * (1 + l.GstPercent / 100m))))
            .ToListAsync(ct);
    }

    public async Task<DeliveryChallanDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var dc = await _context.DeliveryChallans
            .AsNoTracking()
            .Include(d => d.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return dc is null ? null : Map(dc);
    }

    public async Task<DeliveryChallanSaveResult> SaveAsync(DeliveryChallanSaveDto dto, CancellationToken ct = default)
    {
        var errors = ValidateStock(dto.Lines);
        if (errors.Count > 0)
        {
            return new DeliveryChallanSaveResult(false, errors, null);
        }

        DeliveryChallan? dc = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            dc = await _context.DeliveryChallans.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == dto.Id, ct);
        }

        if (dc is null)
        {
            dc = new DeliveryChallan { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.DeliveryChallans.Add(dc);
        }
        else
        {
            _context.DeliveryChallanLines.RemoveRange(dc.Lines);
            dc.Lines.Clear();
        }

        dc.CustomerCompany = dto.CustomerCompany;
        dc.CustomerName = dto.CustomerName;
        dc.CustomerAddress = dto.CustomerAddress;
        dc.CustomerMobile = dto.CustomerMobile;
        dc.DcDate = dto.DcDate;
        dc.Gstin = dto.Gstin;
        dc.Pan = dto.Pan;
        dc.Signatory = dto.Signatory;
        dc.Notes = dto.Notes;
        dc.CompanyName = dto.CompanyName;
        dc.CompanyAddress = dto.CompanyAddress;
        dc.CompanyMobile = dto.CompanyMobile;
        dc.CompanyEmail = dto.CompanyEmail;
        dc.CompanyWebsite = dto.CompanyWebsite;

        foreach (var line in dto.Lines.Where(l => !string.IsNullOrWhiteSpace(l.PartId)))
        {
            dc.Lines.Add(new DeliveryChallanLine
            {
                DcId = dc.Id,
                PartId = line.PartId,
                GstPercent = line.GstPercent,
                Qty = line.Qty,
                Unit = line.Unit,
                Rate = line.Rate,
            });
        }

        await _context.SaveChangesAsync(ct);
        return new DeliveryChallanSaveResult(true, Array.Empty<string>(), dc.Id);
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var ids = await _context.DeliveryChallans.Select(d => d.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("DC-") && int.TryParse(id.AsSpan(3), out var n) ? n : 0)
            .DefaultIfEmpty(11028)
            .Max() + 1;
        return $"DC-{next}";
    }

    public Task<DeliveryChallanDetailDto> CreateBlankAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new DeliveryChallanDetailDto(
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            DateTime.Today, string.Empty, string.Empty, string.Empty, string.Empty,
            LetterheadInfo.CompanyName, LetterheadInfo.CompanyAddress, LetterheadInfo.CompanyMobile,
            LetterheadInfo.CompanyEmail, LetterheadInfo.CompanyWebsite,
            new List<DeliveryChallanLineDto>()));
    }

    public IReadOnlyList<string> ValidateStock(IReadOnlyList<DeliveryChallanLineDto> lines)
    {
        var errors = new List<string>();
        foreach (var line in lines)
        {
            if (line.IsOverage)
            {
                errors.Add($"{line.PartLabel}: quantity {line.Qty} exceeds available stock of {line.AvailableStock}.");
            }
        }
        return errors;
    }

    private static DeliveryChallanDetailDto Map(DeliveryChallan dc) => new(
        dc.Id, dc.CustomerCompany, dc.CustomerName, dc.CustomerAddress, dc.CustomerMobile,
        dc.DcDate, dc.Gstin, dc.Pan, dc.Signatory, dc.Notes,
        dc.CompanyName, dc.CompanyAddress, dc.CompanyMobile, dc.CompanyEmail, dc.CompanyWebsite,
        dc.Lines.Select(l => new DeliveryChallanLineDto(
            l.PartId,
            l.Part is null ? l.PartId : $"{l.Part.Sku} — {l.Part.Name}",
            l.Part?.Stock ?? 0,
            l.GstPercent, l.Qty, l.Unit, l.Rate)).ToList());
}
