using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectNamePlaceholder.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Called by server to send notification to a user
    public async Task SendNotificationToUser(string userId, object notification)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", notification);
    }
}
