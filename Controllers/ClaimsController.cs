using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Claims;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using ClaimEntity = InsuranceClaimsAPI.Models.Domain.Claim;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimsController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        // Support query-based access e.g. GET /api/claims?providerId=6&page=1&pageSize=100
        [HttpGet]
        public async Task<IActionResult> Query([FromQuery] int? providerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            // Minimal support to match frontend usage; pagination can be implemented in service later
            if (providerId.HasValue)
            {
                var claims = await _claimService.GetForProviderAsync(providerId.Value);
                return Ok(new
                {
                    success = true,
                    data = claims,
                    count = claims.Count,
                    page,
                    pageSize
                });
            }

            // If no filters provided, return all claims to support frontend call to /api/claims
            var allClaims = await _claimService.GetAllAsync();
            return Ok(new
            {
                success = true,
                data = allClaims,
                count = allClaims.Count,
                page,
                pageSize
            });
        }

        /// <summary>
        /// Gets all claims (use cautiously, potentially large result set)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var claims = await _claimService.GetAllAsync();
            return Ok(new
            {
                success = true,
                data = claims,
                count = claims.Count
            });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyClaims()
        {
            // Get user ID from header
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault() ?? "0";
            var userId = int.Parse(userIdStr);
            var claims = await _claimService.GetForUserAsync(userId);
            return Ok(claims);
        }

        /// <summary>
        /// Gets all claims for a specific provider
        /// </summary>
        [HttpGet("provider/{providerId:int}")]
        public async Task<IActionResult> GetClaimsForProvider(int providerId)
        {
            try
            {
                var claims = await _claimService.GetForProviderAsync(providerId);
                return Ok(new
                {
                    success = true,
                    data = claims,
                    count = claims.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve claims for provider",
                    details = ex.Message
                });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var claim = await _claimService.GetAsync(id);
            if (claim == null) return NotFound();
            return Ok(claim);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClaimEntity claim)
        {
            var created = await _claimService.CreateAsync(claim);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        /// <summary>
        /// Creates a new claim for a specific provider
        /// </summary>
        [HttpPost("for-provider")]
        public async Task<IActionResult> CreateForProvider([FromBody] CreateClaimForProviderRequest request)
        {
            try
            {
                // Map DTO to Claim entity
                var claim = new ClaimEntity
                {
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    EstimatedAmount = request.EstimatedAmount,
                    PolicyNumber = request.PolicyNumber,
                    PolicyHolderName = request.PolicyHolderName,
                    IncidentLocation = request.IncidentLocation,
                    IncidentDate = request.IncidentDate,
                    DueDate = request.DueDate,
                    Notes = request.Notes,
                    Category = request.Category,
                    ApprovedAmount = 0
                };

                var created = await _claimService.CreateForProviderAsync(request.InsurerId, request.ProviderId, claim);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the claim.", details = ex.Message });
            }
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ClaimStatus status)
        {
            await _claimService.UpdateStatusAsync(id, status);
            return NoContent();
        }
    }
}


