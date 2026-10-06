using Microsoft.AspNetCore.SignalR;
using ProjectNamePlaceholder.Application.Notifications;
using ProjectNamePlaceholder.Application.Notifications.Dtos;

namespace ProjectNamePlaceholder.Api.Hubs;

public sealed class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishAsync(long userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        // The notification is already saved; a failed push just means the client sees it on its next fetch.
        try
        {
            // SignalR's default user id is the NameIdentifier claim, which the JWT sets to the user id.
            await _hubContext.Clients.User(userId.ToString()).SendAsync(NotificationHub.ReceiveNotification, notification, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to push notification {NotificationId} to user {UserId}", notification.Id, userId);
        }
    }
}
