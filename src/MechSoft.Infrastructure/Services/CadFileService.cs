using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Entities;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class CadFileService : ICadFileService
{
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        "exe", "dll", "bat", "cmd", "com", "msi", "scr", "ps1", "vbs", "js", "jse", "wsf", "sh", "jar", "html", "htm", "svg",
    };

    private readonly ApplicationDbContext _context;

    public CadFileService(ApplicationDbContext context) => _context = context;

    public bool IsAllowed(string extension) => !Blocked.Contains(extension);

    public long MaxFileSizeBytes => 50L * 1024 * 1024;

    public async Task<IReadOnlyList<CadFileDto>> ListAllAsync(string? search, CancellationToken ct = default)
    {
        var query = _context.CadFiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f => f.FileName.Contains(term));
        }
        return await query
            .OrderBy(f => f.FileName)
            .Select(f => new CadFileDto(f.Id, f.PartId, f.FileName, f.Extension, f.SizeBytes, f.UploadedAt, f.ApsUrn, f.ApsStatus))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CadFileDto>> ListForPartAsync(string partId, CancellationToken ct = default)
    {
        // Project without Content so listing never pulls the file bytes.
        return await _context.CadFiles
            .AsNoTracking()
            .Where(f => f.PartId == partId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new CadFileDto(f.Id, f.PartId, f.FileName, f.Extension, f.SizeBytes, f.UploadedAt, f.ApsUrn, f.ApsStatus))
            .ToListAsync(ct);
    }

    public async Task<int?> AddAsync(string? partId, string fileName, Stream content, CancellationToken ct = default)
    {
        if (partId is not null && !await _context.Parts.AnyAsync(p => p.Id == partId, ct)) return null;

        var name = fileName.Replace("\\", "/").TrimStart('/'); // may include a folder path
        var ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);

        var file = new CadFile
        {
            PartId = partId,
            FileName = name,
            Extension = ext,
            SizeBytes = ms.Length,
            UploadedAt = DateTime.UtcNow,
            Content = ms.ToArray(),
        };
        _context.CadFiles.Add(file);
        await _context.SaveChangesAsync(ct);
        return file.Id;
    }

    public async Task<CadFileContentDto?> GetContentAsync(int id, CancellationToken ct = default)
    {
        return await _context.CadFiles
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new CadFileContentDto(f.FileName, f.Extension, f.Content))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var file = await _context.CadFiles.FindAsync(new object[] { id }, ct);
        if (file is null) return false;
        _context.CadFiles.Remove(file);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
