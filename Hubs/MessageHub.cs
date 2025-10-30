using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace InsuranceClaimsAPI.Hubs
{
    public class MessageHub : Hub
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(IMemoryCache cache, ILogger<MessageHub> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                
                // Store connection in cache
                _cache.Set($"connection_{userId}", Context.ConnectionId, TimeSpan.FromHours(2));
                
                _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId > 0)
            {
                // Remove connection from cache
                _cache.Remove($"connection_{userId}");
                
                _logger.LogInformation("User {UserId} disconnected", userId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinClaimRoom(int claimId)
        {
            var userId = GetUserId();
            var groupName = $"Claim_{claimId}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("User {UserId} joined claim room {ClaimId}", userId, claimId);
        }

        public async Task LeaveClaimRoom(int claimId)
        {
            var groupName = $"Claim_{claimId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} left claim room {ClaimId}", userId, claimId);
        }

        public async Task JoinQuoteRoom(int quoteId)
        {
            var userId = GetUserId();
            var groupName = $"Quote_{quoteId}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("User {UserId} joined quote room {QuoteId}", userId, quoteId);
        }

        public async Task LeaveQuoteRoom(int quoteId)
        {
            var groupName = $"Quote_{quoteId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} left quote room {QuoteId}", userId, quoteId);
        }

        public async Task SendMessageToClaim(int claimId, string message, string messageType = "Text")
        {
            var userId = GetUserId();
            var username = GetUsername();
            
            var groupName = $"Claim_{claimId}";
            
            // Broadcast to all users in the claim room
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                ClaimId = claimId,
                SenderId = userId,
                SenderName = username,
                Message = message,
                Type = messageType,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Message sent to claim {ClaimId} by user {UserId}", claimId, userId);
        }

        public async Task NotifyQuoteUpdate(int claimId, int quoteId, string updateType)
        {
            var groupName = $"Claim_{claimId}";
            
            await Clients.Group(groupName).SendAsync("QuoteUpdated", new
            {
                ClaimId = claimId,
                QuoteId = quoteId,
                UpdateType = updateType,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyClaimStatusChange(int claimId, string newStatus)
        {
            var groupName = $"Claim_{claimId}";
            
            await Clients.Group(groupName).SendAsync("ClaimStatusChanged", new
            {
                ClaimId = claimId,
                NewStatus = newStatus,
                Timestamp = DateTime.UtcNow
            });
        }

        private int GetUserId()
        {
            // Get user ID from query string (passed when connecting)
            var userIdStr = Context.GetHttpContext()?.Request.Query["userId"].FirstOrDefault();
            return int.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        private string GetUsername()
        {
            // Get username from query string if needed
            return Context.GetHttpContext()?.Request.Query["username"].FirstOrDefault() ?? "Unknown";
        }
    }
}
