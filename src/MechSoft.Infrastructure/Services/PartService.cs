using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Entities;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class PartService : IPartService
{
    private readonly ApplicationDbContext _context;

    public PartService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PartListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _context.Parts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Sku, $"%{term}%") ||
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Material, $"%{term}%") ||
                EF.Functions.Like(p.VendorName, $"%{term}%"));
        }

        return await query
            .OrderBy(p => p.Id)
            .Select(p => new PartListItemDto(p.Id, p.Sku, p.Name, p.Material, p.Category, p.Stock, p.Status, p.Photo != null))
            .ToListAsync(ct);
    }

    public async Task<PartDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var part = await _context.Parts
            .AsNoTracking()
            .Include(p => p.Specs.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (part is null) return null;

        return new PartDetailDto(
            part.Id, part.Sku, part.Name, part.Material, part.Category, part.Stock, part.Status, part.Notes,
            part.Specs.Select(s => new PartSpecDto(s.Label, s.Value, s.SortOrder)).ToList(),
            part.VendorName, part.VendorContactPerson, part.VendorEmail, part.VendorPhone, part.Photo != null);
    }

    public async Task<string> SaveAsync(PartSaveDto dto, CancellationToken ct = default)
    {
        Part? part = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            part = await _context.Parts.Include(p => p.Specs).FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        }

        var isNew = part is null;
        if (part is null)
        {
            part = new Part { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.Parts.Add(part);
        }
        else
        {
            _context.PartSpecs.RemoveRange(part.Specs);
            part.Specs.Clear();
        }

        part.Sku = dto.Sku;
        part.Name = dto.Name;
        part.Material = dto.Material;
        part.Category = dto.Category;
        part.Stock = dto.Stock;
        part.Status = dto.Status;
        part.Notes = dto.Notes;
        part.VendorName = dto.VendorName;
        part.VendorContactPerson = dto.VendorContactPerson;
        part.VendorEmail = dto.VendorEmail;
        part.VendorPhone = dto.VendorPhone;

        var order = 0;
        foreach (var spec in dto.Specs.Where(s => !string.IsNullOrWhiteSpace(s.Label)))
        {
            part.Specs.Add(new PartSpec { PartId = part.Id, Label = spec.Label, Value = spec.Value, SortOrder = order++ });
        }

        await _context.SaveChangesAsync(ct);
        return part.Id;
    }

    public async Task<bool> UpdateStockAsync(string id, int stock, MechSoft.Domain.Enums.StockStatus status, CancellationToken ct = default)
    {
        var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null) return false;
        part.Stock = Math.Max(0, stock);
        part.Status = status;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdatePhotoAsync(string id, string contentType, byte[] content, CancellationToken ct = default)
    {
        var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null) return false;
        part.Photo = content;
        part.PhotoContentType = contentType;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PhotoContentDto?> GetPhotoAsync(string id, CancellationToken ct = default)
    {
        var part = await _context.Parts
            .AsNoTracking()
            .Where(p => p.Id == id && p.Photo != null)
            .Select(p => new { p.Photo, p.PhotoContentType })
            .FirstOrDefaultAsync(ct);

        return part?.Photo is null ? null : new PhotoContentDto(part.PhotoContentType ?? "application/octet-stream", part.Photo);
    }

    public async Task<bool> RemovePhotoAsync(string id, CancellationToken ct = default)
    {
        var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null) return false;
        part.Photo = null;
        part.PhotoContentType = null;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var maxNumeric = await _context.Parts
            .Select(p => p.Id)
            .ToListAsync(ct);

        var next = maxNumeric
            .Select(id => id.StartsWith("P-") && int.TryParse(id.AsSpan(2), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"P-{next:D3}";
    }
}
