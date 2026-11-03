using System.Net.Mail;
using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Entities;
using MechSoft.Domain.Enums;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class PartService : IPartService
{
    private readonly ApplicationDbContext _context;

    public PartService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PartListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        // REQ-06: the match must be literal (SQL LIKE's %, _, [ ] wildcards must NOT apply), case
        // insensitive, and trimmed. The catalogue is small, so filtering in memory with an ordinal
        // (non-wildcard) Contains is simpler and more reliably literal than escaping LIKE patterns.
        var all = await _context.Parts
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PartListItemDto(p.Id, p.Sku, p.Name, p.Material, p.Category, p.VendorName, p.Stock, p.Status, p.Photo != null, p.Version))
            .ToListAsync(ct);

        var term = search?.Trim();
        if (string.IsNullOrEmpty(term)) return all;

        return all.Where(p => Matches(p.Sku, term) || Matches(p.Name, term) || Matches(p.Material, term) || Matches(p.VendorName, term)).ToList();
    }

    private static bool Matches(string field, string term) => field.Contains(term, StringComparison.OrdinalIgnoreCase);

    public async Task<PartDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var part = await _context.Parts
            .AsNoTracking()
            .Include(p => p.Specs.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return part is null ? null : Map(part);
    }

    public async Task<PartSaveResult> CreateAsync(PartSaveDto dto, CancellationToken ct = default)
    {
        var (trimmed, errors) = ValidateAndTrim(dto);
        if (errors.Count > 0) return PartSaveResult.Invalid(errors);

        var part = new Part
        {
            Id = await GenerateNextIdAsync(ct),
            Stock = 0,
            Status = StockStatus.OutOfStock,
            Version = 1,
        };
        Apply(part, trimmed);

        _context.Parts.Add(part);
        await _context.SaveChangesAsync(ct);
        return PartSaveResult.Success(part.Id);
    }

    public async Task<PartSaveResult> UpdateAsync(string id, int version, PartSaveDto dto, CancellationToken ct = default)
    {
        var (trimmed, errors) = ValidateAndTrim(dto);
        if (errors.Count > 0) return PartSaveResult.Invalid(errors);

        var part = await _context.Parts.Include(p => p.Specs).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null) return PartSaveResult.NotFoundResult;
        if (part.Version != version) return PartSaveResult.ConflictResult;

        _context.PartSpecs.RemoveRange(part.Specs);
        part.Specs.Clear();
        Apply(part, trimmed);
        part.Version++; // stock/status are deliberately untouched here (REQ-04)

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PartSaveResult.ConflictResult;
        }

        return PartSaveResult.Success(part.Id);
    }

    public async Task<StockUpdateResult> UpdateStockAsync(string id, int version, int stock, StockStatus status, CancellationToken ct = default)
    {
        var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (part is null) return new StockUpdateResult(StockUpdateOutcome.NotFound, 0, StockStatus.OutOfStock, 0);
        if (part.Version != version) return new StockUpdateResult(StockUpdateOutcome.Conflict, part.Stock, part.Status, part.Version);

        part.Stock = Math.Max(0, stock);
        part.Status = status;
        part.Version++;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await _context.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return current is null
                ? new StockUpdateResult(StockUpdateOutcome.NotFound, 0, StockStatus.OutOfStock, 0)
                : new StockUpdateResult(StockUpdateOutcome.Conflict, current.Stock, current.Status, current.Version);
        }

        return new StockUpdateResult(StockUpdateOutcome.Success, part.Stock, part.Status, part.Version);
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
        var ids = await _context.Parts.Select(p => p.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("P-") && int.TryParse(id.AsSpan(2), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"P-{next:D3}";
    }

    private static void Apply(Part part, PartSaveDto dto)
    {
        part.Sku = dto.Sku;
        part.Name = dto.Name;
        part.Material = dto.Material;
        part.Category = dto.Category;
        part.Notes = dto.Notes;
        part.VendorName = dto.VendorName;
        part.VendorContactPerson = dto.VendorContactPerson;
        part.VendorEmail = dto.VendorEmail;
        part.VendorPhone = dto.VendorPhone;

        var order = 0;
        foreach (var spec in dto.Specs)
        {
            part.Specs.Add(new PartSpec { PartId = part.Id, Label = spec.Label, Value = spec.Value, SortOrder = order++ });
        }
    }

    private static PartDetailDto Map(Part part) => new(
        part.Id, part.Sku, part.Name, part.Material, part.Category, part.Stock, part.Status, part.Notes,
        part.Specs.Select(s => new PartSpecDto(s.Label, s.Value, s.SortOrder)).ToList(),
        part.VendorName, part.VendorContactPerson, part.VendorEmail, part.VendorPhone, part.Photo != null, part.Version);

    // ---------------------------------------------------------------------
    // REQ-07: every text field is trimmed first; required/length/email/spec
    // checks then run on the trimmed values, so length is counted after trim.
    // REQ-03: strict vendor email validation.
    // ---------------------------------------------------------------------

    private static (PartSaveDto Trimmed, List<PartFieldError> Errors) ValidateAndTrim(PartSaveDto dto)
    {
        var errors = new List<PartFieldError>();
        static string Trim(string? s) => (s ?? string.Empty).Trim();

        var sku = Trim(dto.Sku);
        var name = Trim(dto.Name);
        var material = Trim(dto.Material);
        var category = Trim(dto.Category);
        var vendorName = Trim(dto.VendorName);
        var vendorContact = Trim(dto.VendorContactPerson);
        var vendorEmail = Trim(dto.VendorEmail);
        var vendorPhone = Trim(dto.VendorPhone);
        // Notes keeps its inner whitespace/line breaks; only the outer edges are trimmed.
        var notes = Trim(dto.Notes);

        void Required(string field, string value, string label)
        {
            if (value.Length == 0) errors.Add(new PartFieldError(field, $"{label} is required."));
        }
        void MaxLen(string field, string value, int max, string label)
        {
            if (value.Length > max) errors.Add(new PartFieldError(field, $"{label} must be {max} characters or fewer."));
        }

        Required("Sku", sku, "SKU");
        MaxLen("Sku", sku, 50, "SKU");
        Required("Name", name, "Part Name");
        MaxLen("Name", name, 200, "Part Name");
        Required("Material", material, "Material");
        MaxLen("Material", material, 100, "Material");
        Required("Category", category, "Category");
        MaxLen("Category", category, 100, "Category");
        MaxLen("VendorName", vendorName, 200, "Vendor name");
        MaxLen("VendorContactPerson", vendorContact, 100, "Contact person");
        MaxLen("VendorEmail", vendorEmail, 200, "Vendor email");
        MaxLen("VendorPhone", vendorPhone, 30, "Vendor phone");
        MaxLen("Notes", notes, 2000, "Notes");

        if (!IsValidVendorEmail(vendorEmail))
        {
            errors.Add(new PartFieldError("VendorEmail", "Enter a valid vendor email address."));
        }

        var specs = new List<PartSpecDto>();
        var specOrder = 0;
        foreach (var spec in dto.Specs)
        {
            var label = Trim(spec.Label);
            if (label.Length == 0) continue; // blank-label rows are dropped, not saved

            var value = Trim(spec.Value);
            MaxLen($"Specs[{specOrder}].Label", label, 100, "Specification label");
            MaxLen($"Specs[{specOrder}].Value", value, 200, "Specification value");
            specs.Add(new PartSpecDto(label, value, specOrder++));
        }

        var trimmedDto = new PartSaveDto(sku, name, material, category, notes.Length == 0 ? null : notes, specs,
            vendorName, vendorContact, vendorEmail, vendorPhone);
        return (trimmedDto, errors);
    }

    /// <summary>
    /// REQ-03: empty is valid (optional field). Otherwise MailAddress.TryCreate must succeed and the
    /// parsed address must equal the trimmed input exactly (rejects display names, angle brackets,
    /// quotes, comments and anything with more than one address), it must contain no whitespace, the
    /// domain must contain at least one dot, and the whole value must be at most 200 characters.
    /// </summary>
    private static bool IsValidVendorEmail(string email)
    {
        if (email.Length == 0) return true;
        if (email.Length > 200) return false;
        if (email.Any(char.IsWhiteSpace)) return false;
        if (!MailAddress.TryCreate(email, out var parsed)) return false;
        if (!string.Equals(parsed.Address, email, StringComparison.Ordinal)) return false;

        var at = parsed.Address.IndexOf('@');
        if (at < 0 || at == parsed.Address.Length - 1) return false;
        var domain = parsed.Address[(at + 1)..];
        return domain.Contains('.');
    }
}
