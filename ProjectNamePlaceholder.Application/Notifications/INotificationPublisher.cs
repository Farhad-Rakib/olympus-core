using ProjectNamePlaceholder.Application.Notifications.Dtos;

namespace ProjectNamePlaceholder.Application.Notifications;

/// <summary>Pushes a saved notification to the recipient's connected clients in real time.</summary>
public interface INotificationPublisher
{
    Task PublishAsync(long userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
