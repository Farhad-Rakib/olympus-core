using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(long userId, int take, CancellationToken cancellationToken = default);
    Task<Notification?> GetForUserByIdAsync(long userId, long notificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(long userId, CancellationToken cancellationToken = default);
}
