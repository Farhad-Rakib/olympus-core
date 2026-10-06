using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public sealed record DailyRequestCount(DateTime Date, int Requests, int Failed);

public sealed record DashboardSnapshot(
    int TotalUsers,
    int ActiveUsers,
    int NewUsers,
    int TotalRoles,
    IReadOnlyList<DailyRequestCount> DailyRequests,
    IReadOnlyList<AuditLog> RecentActivity);

public interface IDashboardRepository
{
    /// <summary>Counts and audit activity since <paramref name="fromUtc"/>; recent activity covers state-changing requests only.</summary>
    Task<DashboardSnapshot> GetSnapshotAsync(DateTime fromUtc, int recentActivityCount, CancellationToken cancellationToken = default);
}
