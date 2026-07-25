using Microsoft.AspNetCore.SignalR;
using MyDesk.Web.Services;
using System.Threading.Tasks;

namespace MyDesk.Web.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly SignalRNotificationService _notificationService;

        public NotificationHub(SignalRNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            // Optionally, you can add the user to a group based on their tenant or user ID
            var userId = Context.User?.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}