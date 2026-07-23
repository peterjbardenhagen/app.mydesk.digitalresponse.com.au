using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MyDesk.Web.Hubs
{
    /// <summary>
    /// SignalR hub for real-time notifications.
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Sends a notification to a specific user.
        /// </summary>
        /// <param name="userId">The user ID to send the notification to.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendToUser(string userId, string title, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }

        /// <summary>
        /// Sends a notification to all users in a tenant (by sending to a group named after the tenant).
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendToTenant(string tenantId, string title, string message)
        {
            await Clients.Group(tenantId).SendAsync("ReceiveNotification", title, message);
        }

        /// <summary>
        /// Sends a notification to multiple users.
        /// </summary>
        /// <param name="userIds">The user IDs to send the notification to.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendToUsers(string[] userIds, string title, string message)
        {
            foreach (var userId in userIds)
            {
                await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
            }
        }

        /// <summary>
        /// Override to add the user to a group based on their tenant when they connect.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            // Get the tenant ID from the user's claims (if available)
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Override to remove the user from the tenant group when they disconnect.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Get the tenant ID from the user's claims (if available)
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}