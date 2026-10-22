using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Enums;
using MechSoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechSoft.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context) => _context = context;

    public async Task<DashboardViewDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var activeSkus = await _context.Parts.CountAsync(ct);
        var lowOrOut = await _context.Parts.CountAsync(p => p.Status != StockStatus.InStock, ct);
        var openPoCount = await _context.PurchaseOrders.CountAsync(ct);
        var dcCount = await _context.DeliveryChallans.CountAsync(ct);

        var attention = await _context.Parts
            .AsNoTracking()
            .Where(p => p.Status != StockStatus.InStock)
            .OrderBy(p => p.Stock)
            .Select(p => new PartListItemDto(p.Id, p.Sku, p.Name, p.Material, p.Category, p.Stock, p.Status, p.Photo != null))
            .ToListAsync(ct);

        var recentDcs = await _context.DeliveryChallans
            .AsNoTracking()
            .Include(d => d.Lines)
            .OrderByDescending(d => d.DcDate)
            .Take(5)
            .Select(d => new RecentDocumentDto(d.Id, "Delivery Challan", d.CustomerCompany, d.DcDate,
                d.Lines.Sum(l => l.Qty * l.Rate * (1 + l.GstPercent / 100m))))
            .ToListAsync(ct);

        var recentPos = await _context.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(p => p.PoDate)
            .Take(5)
            .Select(p => new RecentDocumentDto(p.Id, "Purchase Order", p.VendorCompany, p.PoDate, p.TotalValue))
            .ToListAsync(ct);

        var recentDocs = recentDcs.Concat(recentPos)
            .OrderByDescending(d => d.Date)
            .Take(5)
            .ToList();

        var stats = new DashboardStatsDto(activeSkus, lowOrOut, openPoCount, dcCount);

        var ongoingCount = await _context.Projects.CountAsync(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.OnHold, ct);
        var completedCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed, ct);
        var onHoldCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.OnHold, ct);
        var avgCompletion = await _context.Projects
            .Where(p => p.Status != ProjectStatus.Completed)
            .Select(p => (double?)p.CompletionPercent)
            .AverageAsync(ct) ?? 0;

        var projectSummary = new ProjectSummaryDto(ongoingCount, completedCount, onHoldCount, avgCompletion);

        var ongoingProjects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Status != ProjectStatus.Completed)
            .OrderBy(p => p.TargetDate ?? DateTime.MaxValue)
            .Take(6)
            .Select(p => new ProjectListItemDto(p.Id, p.Name, p.MachineType, p.CustomerName, p.StartDate, p.TargetDate, p.Status, p.CompletionPercent, p.Photo != null))
            .ToListAsync(ct);

        return new DashboardViewDto(stats, attention, recentDocs, projectSummary, ongoingProjects);
    }
}
