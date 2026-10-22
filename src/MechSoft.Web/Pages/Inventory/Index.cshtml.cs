using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using MechSoft.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.Inventory;

public class IndexModel : PageModel
{
    private readonly IPartService _partService;

    public IndexModel(IPartService partService) => _partService = partService;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>Optional status filter; null shows everything.</summary>
    [BindProperty(SupportsGet = true)]
    public StockStatus? Status { get; set; }

    public IReadOnlyList<PartListItemDto> AllParts { get; private set; } = Array.Empty<PartListItemDto>();
    public IReadOnlyList<PartListItemDto> Parts { get; private set; } = Array.Empty<PartListItemDto>();

    public int TotalUnits => AllParts.Sum(p => p.Stock);
    public int Count(StockStatus s) => AllParts.Count(p => p.Status == s);

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        AllParts = await _partService.GetAllAsync(Search, ct);
        Parts = Status is { } s ? AllParts.Where(p => p.Status == s).ToList() : AllParts;
    }

    public async Task<IActionResult> OnPostUpdateStockAsync(string id, int stock, StockStatus status, string? search, StockStatus? filter, CancellationToken ct)
    {
        if (stock < 0)
        {
            Message = "Stock cannot be negative.";
        }
        else if (await _partService.UpdateStockAsync(id, stock, status, ct))
        {
            Message = $"Updated stock for {id}.";
        }
        return RedirectToPage(new { search, status = filter });
    }
}
