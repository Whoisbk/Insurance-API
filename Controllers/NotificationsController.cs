using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimsAPI.Data;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly InsuranceClaimsContext _context;
        private readonly INotificationService _notificationService;

        public NotificationsController(InsuranceClaimsContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            // Get user ID from header
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault() ?? "0";
            var userId = int.Parse(userIdStr);
            var items = await _context.Notifications
                .Include(n => n.Quote)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateSent)
                .Take(100)
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>
        /// Gets all notifications for a specific provider
        /// </summary>
        [HttpGet("provider/{providerId:int}")]
        public async Task<IActionResult> GetByProvider(int providerId)
        {
            try
            {
                // Validate that the provider exists
                var provider = await _context.Users
                    .Where(u => u.DeletedAt == null)
                    .FirstOrDefaultAsync(u => u.Id == providerId && u.Role == UserRole.Provider);

                if (provider == null)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

                var notifications = await _context.Notifications
                    .Include(n => n.Quote)
                    .Where(n => n.UserId == providerId)
                    .OrderByDescending(n => n.DateSent)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications.Select(n => new
                    {
                        notificationId = n.NotificationId,
                        userId = n.UserId,
                        quoteId = n.QuoteId,
                        message = n.Message,
                        dateSent = n.DateSent,
                        status = n.Status.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve provider notifications",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all notifications for a specific insurer
        /// </summary>
        [HttpGet("insurer/{insurerId:int}")]
        public async Task<IActionResult> GetByInsurer(int insurerId)
        {
            try
            {
                var insurer = await _context.Users
                    .Where(u => u.DeletedAt == null)
                    .FirstOrDefaultAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);

                if (insurer == null)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                var notifications = await _context.Notifications
                    .Include(n => n.Quote)
                    .Where(n => n.UserId == insurerId)
                    .OrderByDescending(n => n.DateSent)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications.Select(n => new
                    {
                        notificationId = n.NotificationId,
                        userId = n.UserId,
                        quoteId = n.QuoteId,
                        message = n.Message,
                        dateSent = n.DateSent,
                        status = n.Status.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve insurer notifications",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Marks a notification as read for the authenticated user
        /// </summary>
        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            try
            {
                // Get user ID from header
                var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return BadRequest(new { success = false, error = "User ID header (X-User-Id) is required" });
                }

                // Validate that the notification exists and belongs to the user
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new { success = false, error = "Notification not found or access denied" });
                }

                await _notificationService.MarkAsReadAsync(id, userId);

                return Ok(new
                {
                    success = true,
                    message = "Notification marked as read",
                    notificationId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to mark notification as read",
                    details = ex.Message
                });
            }
        }
    }
}


