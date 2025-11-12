using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/providers/{providerId}/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly InsuranceClaimsContext _context;

        public ActivitiesController(InsuranceClaimsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int providerId, int take = 3)
        {
            var activities = await _context.AuditLogs
                .Where(a => a.UserId == providerId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .Select(a => new RecentActivityDto
                {
                    Action = a.Action.ToString(),
                    EntityType = a.EntityType.ToString(),
                    ActionDescription = a.ActionDescription,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(activities);
        }
    }
}
