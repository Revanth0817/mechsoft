using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.PurchaseOrders;

public class IndexModel : PageModel
{
    private readonly IPurchaseOrderService _service;

    public IndexModel(IPurchaseOrderService service) => _service = service;

    public IReadOnlyList<PurchaseOrderListItemDto> Orders { get; private set; } = Array.Empty<PurchaseOrderListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Orders = await _service.GetAllAsync(ct);
    }
}
