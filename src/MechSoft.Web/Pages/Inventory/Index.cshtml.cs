using System.Globalization;
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

    /// <summary>Every part. Filtering, searching, sorting and paging all happen client-side; the view
    /// state itself lives in the URL query string (REQ-01), not in anything the server tracks.</summary>
    public IReadOnlyList<PartListItemDto> Parts { get; private set; } = Array.Empty<PartListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Parts = await _partService.GetAllAsync(null, ct);
    }

    /// <summary>
    /// AJAX stock/status update used by the inventory table's inline "Save". Returns JSON rather than
    /// redirecting, so the page can update the row in place without a full reload. Version-checked
    /// (REQ-02): a missing or non-numeric version is a 400; a stale version is a 409 with the part's
    /// current stock, status and version so the row can offer "Use latest values".
    /// </summary>
    public async Task<IActionResult> OnPostUpdateStockAsync([FromForm] string id, [FromForm] string? version, [FromForm] int stock, [FromForm] StockStatus status, CancellationToken ct)
    {
        if (!int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expectedVersion))
        {
            return BadRequest(new { success = false, error = "Invalid request. Nothing was saved." });
        }

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

        var result = await _partService.UpdateStockAsync(id, expectedVersion, stock, status, ct);

        switch (result.Outcome)
        {
            case StockUpdateOutcome.NotFound:
                return new JsonResult(new { success = false, error = "That part no longer exists." });

            case StockUpdateOutcome.Conflict:
                var statusLabel = StatusLabel(result.Status);
                var units = result.Stock.ToString("N0", CultureInfo.GetCultureInfo("en-IN"));
                var message = $"This part was changed by someone else (now {units} units, {statusLabel}). " +
                              "Your change was not saved. Select Use latest values to continue.";
                return new ObjectResult(new
                {
                    success = false,
                    conflict = true,
                    error = message,
                    stock = result.Stock,
                    status = (int)result.Status,
                    version = result.Version,
                })
                { StatusCode = StatusCodes.Status409Conflict };

            default:
                return new JsonResult(new { success = true, id, stock = result.Stock, status = (int)result.Status, version = result.Version });
        }
    }

    private static string StatusLabel(StockStatus status) => status switch
    {
        StockStatus.InStock => "In stock",
        StockStatus.LowStock => "Low stock",
        _ => "Out of stock",
    };
}
