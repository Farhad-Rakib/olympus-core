using ProjectNamePlaceholder.Application.Dashboard.Dtos;

namespace ProjectNamePlaceholder.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int days, CancellationToken cancellationToken = default);
}
