using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectNamePlaceholder.Api.Hubs;

/// <summary>
/// Server-to-client notification channel. Clients only listen for "ReceiveNotification";
/// notifications are created through the REST API, never by clients invoking the hub.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public const string ReceiveNotification = "ReceiveNotification";
}
