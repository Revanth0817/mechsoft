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

    /// <summary>Every part. Filtering, searching, sorting and paging all happen client-side.</summary>
    public IReadOnlyList<PartListItemDto> Parts { get; private set; } = Array.Empty<PartListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Parts = await _partService.GetAllAsync(null, ct);
    }

    /// <summary>
    /// AJAX stock/status update used by the inventory table's inline "Save". Returns JSON rather
    /// than redirecting, so the page can update the row in place without a full reload.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateStockAsync([FromForm] string id, [FromForm] int stock, [FromForm] StockStatus status, CancellationToken ct)
    {
        if (stock < 0)
        {
            return new JsonResult(new { success = false, error = "Enter a whole number of 0 or more." });
        }

        // Stock 0 only ever means Out of Stock, and Out of Stock only ever means 0 on hand.
        if ((stock == 0) != (status == StockStatus.OutOfStock))
        {
            return new JsonResult(new
            {
                success = false,
                error = stock == 0 ? "Stock 0 can only be saved as Out of stock." : "Out of stock needs a stock of 0.",
            });
        }

        var ok = await _partService.UpdateStockAsync(id, stock, status, ct);
        if (!ok)
        {
            return new JsonResult(new { success = false, error = "That part no longer exists." });
        }

        return new JsonResult(new { success = true, id, stock, status = (int)status });
    }
}
