using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClaimEntity = InsuranceClaimsAPI.Models.Domain.Claim;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimsController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyClaims()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            var userId = int.Parse(userIdStr);
            var claims = await _claimService.GetForUserAsync(userId);
            return Ok(claims);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var claim = await _claimService.GetAsync(id);
            if (claim == null) return NotFound();
            return Ok(claim);
        }

        [HttpPost]
        [Authorize(Policy = "RequireProviderRole")]
        public async Task<IActionResult> Create([FromBody] ClaimEntity claim)
        {
            var created = await _claimService.CreateAsync(claim);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Policy = "RequireInsurerRole")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ClaimStatus status)
        {
            await _claimService.UpdateStatusAsync(id, status);
            return NoContent();
        }
    }
}


