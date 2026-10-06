using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(long userId, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Notification?> GetForUserByIdAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
    }
}
