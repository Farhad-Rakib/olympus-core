using ProjectNamePlaceholder.Application.Notifications.Dtos;

namespace ProjectNamePlaceholder.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteNotificationAsync(long userId, long notificationId, CancellationToken cancellationToken = default);
}
