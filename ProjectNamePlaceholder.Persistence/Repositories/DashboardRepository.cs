using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(DateTime fromUtc, int recentActivityCount, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users.AsNoTracking();
        var totalUsers = await users.CountAsync(cancellationToken);
        var activeUsers = await users.CountAsync(u => u.IsActive, cancellationToken);
        var newUsers = await users.CountAsync(u => u.CreatedAt >= fromUtc, cancellationToken);
        var totalRoles = await _dbContext.Roles.CountAsync(cancellationToken);

        var audit = _dbContext.AuditLogs.AsNoTracking().Where(a => a.Timestamp >= fromUtc);

        var daily = await audit
            .GroupBy(a => a.Timestamp.Date)
            .Select(g => new DailyRequestCount(g.Key, g.Count(), g.Count(a => !a.Success)))
            .ToListAsync(cancellationToken);

        // Reads are counted above but left out of the feed, which would otherwise be page-load noise.
        var recent = await audit
            .Where(a => a.Method != "GET")
            .OrderByDescending(a => a.Timestamp)
            .Take(recentActivityCount)
            .ToListAsync(cancellationToken);

        return new DashboardSnapshot(totalUsers, activeUsers, newUsers, totalRoles, daily, recent);
    }
}
