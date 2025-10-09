using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimsAPI.Data;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateSent)
                .Take(100)
                .ToListAsync();
            return Ok(items);
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }
    }
}


