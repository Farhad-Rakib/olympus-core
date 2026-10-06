namespace ProjectNamePlaceholder.Application.Dashboard.Dtos;

public sealed record DashboardStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int NewUsers,
    int TotalRoles,
    int Requests,
    int FailedRequests);

public sealed record DailyActivityDto(DateOnly Date, int Requests, int Failed);

public sealed record RecentActivityDto(
    long Id,
    string User,
    string Action,
    string Method,
    int? StatusCode,
    bool Success,
    DateTime Timestamp);

public sealed record DashboardDto(
    int Days,
    DashboardStatsDto Stats,
    IReadOnlyList<DailyActivityDto> Activity,
    IReadOnlyList<RecentActivityDto> RecentActivity);
