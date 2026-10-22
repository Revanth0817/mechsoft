using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Entities;
using MechSoft.Domain.Enums;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectListItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _context.Projects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.MachineType, $"%{term}%") ||
                EF.Functions.Like(p.CustomerName, $"%{term}%"));
        }

        return await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new ProjectListItemDto(p.Id, p.Name, p.MachineType, p.CustomerName, p.StartDate, p.TargetDate, p.Status, p.CompletionPercent, p.Photo != null))
            .ToListAsync(ct);
    }

    public async Task<ProjectDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var p = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return p is null ? null : Map(p);
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetOngoingAsync(int take, CancellationToken ct = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Status != ProjectStatus.Completed)
            .OrderBy(p => p.TargetDate ?? DateTime.MaxValue)
            .Take(take)
            .Select(p => new ProjectListItemDto(p.Id, p.Name, p.MachineType, p.CustomerName, p.StartDate, p.TargetDate, p.Status, p.CompletionPercent, p.Photo != null))
            .ToListAsync(ct);
    }

    public async Task<string> SaveAsync(ProjectSaveDto dto, CancellationToken ct = default)
    {
        Project? project = null;
        if (!string.IsNullOrWhiteSpace(dto.Id))
        {
            project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        }

        if (project is null)
        {
            project = new Project { Id = string.IsNullOrWhiteSpace(dto.Id) ? await GenerateNextIdAsync(ct) : dto.Id! };
            _context.Projects.Add(project);
        }

        project.Name = dto.Name;
        project.MachineType = dto.MachineType;
        project.CustomerName = dto.CustomerName;
        project.StartDate = dto.StartDate;
        project.TargetDate = dto.TargetDate;
        project.Status = dto.Status;
        project.CompletionPercent = Math.Clamp(dto.CompletionPercent, 0, 100);
        project.Notes = dto.Notes;

        // Completion follows status at the edges, so the two never visibly disagree.
        if (project.Status == ProjectStatus.Completed) project.CompletionPercent = 100;
        else if (project.Status == ProjectStatus.Planning && project.CompletionPercent == 0) { /* leave as-is */ }

        await _context.SaveChangesAsync(ct);
        return project.Id;
    }

    public async Task<string> GenerateNextIdAsync(CancellationToken ct = default)
    {
        var ids = await _context.Projects.Select(p => p.Id).ToListAsync(ct);
        var next = ids
            .Select(id => id.StartsWith("PRJ-") && int.TryParse(id.AsSpan(4), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return $"PRJ-{next:D3}";
    }

    public async Task<bool> UpdatePhotoAsync(string id, string contentType, byte[] content, CancellationToken ct = default)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return false;
        project.Photo = content;
        project.PhotoContentType = contentType;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PhotoContentDto?> GetPhotoAsync(string id, CancellationToken ct = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == id && p.Photo != null)
            .Select(p => new { p.Photo, p.PhotoContentType })
            .FirstOrDefaultAsync(ct);

        return project?.Photo is null ? null : new PhotoContentDto(project.PhotoContentType ?? "application/octet-stream", project.Photo);
    }

    public async Task<bool> RemovePhotoAsync(string id, CancellationToken ct = default)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return false;
        project.Photo = null;
        project.PhotoContentType = null;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static ProjectDetailDto Map(Project p) => new(
        p.Id, p.Name, p.MachineType, p.CustomerName, p.StartDate, p.TargetDate, p.Status, p.CompletionPercent, p.Notes, p.Photo != null);
}
