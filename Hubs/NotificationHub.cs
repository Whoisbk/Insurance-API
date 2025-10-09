using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InsuranceClaimsAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public async Task SendToUser(int userId, string title, string message)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        private int GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}


