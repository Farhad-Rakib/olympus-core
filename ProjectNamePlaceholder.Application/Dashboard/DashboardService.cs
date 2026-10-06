using System.Text.RegularExpressions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Dashboard.Dtos;

namespace ProjectNamePlaceholder.Application.Dashboard;

public sealed partial class DashboardService : IDashboardService
{
    public const int MinDays = 1;
    public const int MaxDays = 365;
    private const int RecentActivityCount = 10;

    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync(int days, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, MinDays, MaxDays);
        var today = DateTime.UtcNow.Date;
        var fromUtc = today.AddDays(-(days - 1));

        var snapshot = await _dashboardRepository.GetSnapshotAsync(fromUtc, RecentActivityCount, cancellationToken);

        // Include days without traffic so the chart has a continuous x-axis.
        var byDay = snapshot.DailyRequests.ToDictionary(d => d.Date.Date);
        var activity = Enumerable.Range(0, days)
            .Select(offset => fromUtc.AddDays(offset))
            .Select(day => byDay.TryGetValue(day, out var c)
                ? new DailyActivityDto(DateOnly.FromDateTime(day), c.Requests, c.Failed)
                : new DailyActivityDto(DateOnly.FromDateTime(day), 0, 0))
            .ToList();

        var stats = new DashboardStatsDto(
            snapshot.TotalUsers,
            snapshot.ActiveUsers,
            snapshot.NewUsers,
            snapshot.TotalRoles,
            activity.Sum(a => a.Requests),
            activity.Sum(a => a.Failed));

        var recent = snapshot.RecentActivity
            .Select(a => new RecentActivityDto(
                a.Id,
                a.UserName ?? (a.UserId.HasValue ? $"User #{a.UserId}" : "Anonymous"),
                FormatAction(a.Action),
                a.Method,
                a.StatusCode,
                a.Success,
                a.Timestamp))
            .ToList();

        return new DashboardDto(days, stats, activity, recent);
    }

    /// <summary>
    /// Turns "Namespace.UsersController.GetProfile (Assembly)" into "Users: Get Profile".
    /// </summary>
    public static string FormatAction(string action)
    {
        var match = ActionPattern().Match(action ?? string.Empty);
        if (!match.Success) return action ?? string.Empty;

        var method = SplitWords().Replace(match.Groups["method"].Value, " $1").Trim();
        return $"{match.Groups["controller"].Value}: {method}";
    }

    [GeneratedRegex(@"\.(?<controller>\w+)Controller\.(?<method>\w+)")]
    private static partial Regex ActionPattern();

    [GeneratedRegex(@"(?<!^)([A-Z])")]
    private static partial Regex SplitWords();
}
