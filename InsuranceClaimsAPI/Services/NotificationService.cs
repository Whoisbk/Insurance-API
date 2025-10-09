using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimsAPI.Hubs;

namespace InsuranceClaimsAPI.Services
{
    public interface INotificationService
    {
        Task<int> CreateAsync(Notification notification);
        Task MarkAsReadAsync(int notificationId, int userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(InsuranceClaimsContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<int> CreateAsync(Notification notification)
        {
            notification.DateSent = DateTime.UtcNow;
            notification.Status = NotificationStatus.Unread;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("ReceiveNotification", new
            {
                Message = notification.Message,
                Status = notification.Status.ToString(),
                Timestamp = DateTime.UtcNow
            });

            return notification.NotificationId;
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return;
            }

            notification.Status = NotificationStatus.Read;
            await _context.SaveChangesAsync();
        }
    }
}


