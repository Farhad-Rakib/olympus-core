using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<(IReadOnlyList<AuditLog> Items, long Total)> QueryAsync(
        long? userId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        bool? success = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action))
        {
            // StringComparison overloads cannot be translated to SQL; lower-case both sides instead.
            var term = action.Trim().ToLower();
            query = query.Where(x => x.Action.ToLower().Contains(term) || x.Path.ToLower().Contains(term));
        }
        if (from.HasValue) query = query.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Timestamp <= to.Value);
        if (success.HasValue) query = query.Where(x => x.Success == success.Value);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
