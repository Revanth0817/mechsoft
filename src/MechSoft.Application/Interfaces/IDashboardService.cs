using MechSoft.Application.DTOs;

namespace MechSoft.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewDto> GetDashboardAsync(CancellationToken ct = default);
}
