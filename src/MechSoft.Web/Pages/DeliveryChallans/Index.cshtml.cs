using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages.DeliveryChallans;

public class IndexModel : PageModel
{
    private readonly IDeliveryChallanService _service;

    public IndexModel(IDeliveryChallanService service) => _service = service;

    public IReadOnlyList<DeliveryChallanListItemDto> Challans { get; private set; } = Array.Empty<DeliveryChallanListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Challans = await _service.GetAllAsync(ct);
    }
}
