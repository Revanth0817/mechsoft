using MechSoft.Application.DTOs;
using MechSoft.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MechSoft.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(IDashboardService dashboardService) => _dashboardService = dashboardService;

    public DashboardViewDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Dashboard = await _dashboardService.GetDashboardAsync(ct);
    }
}
