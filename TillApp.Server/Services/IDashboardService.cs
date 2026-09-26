using TillApp.Shared.Dashboard;

namespace TillApp.Server.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
