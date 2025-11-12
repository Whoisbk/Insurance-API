using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Claims;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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

                var data = claims.Select(c => new
                {
                    claimId = c.Id,
                    claimNumber = c.ClaimNumber,
                    title = c.Title,
                    description = c.Description,
                    clientName = c.ClientFullName,
                    clientFullName = c.ClientFullName,
                    clientEmailAddress = c.ClientEmailAddress,
                    clientPhoneNumber = c.ClientPhoneNumber,
                    clientAddress = c.ClientAddress,
                    clientCompany = c.ClientCompany,
                    status = c.Status.ToString(),
                    priority = c.Priority.ToString(),
                    estimatedAmount = c.EstimatedAmount,
                    approvedAmount = c.ApprovedAmount,
                    policyNumber = c.PolicyNumber,
                    policyHolderName = c.PolicyHolderName,
                    incidentDate = c.IncidentDate,
                    incidentLocation = c.IncidentLocation,
                    dueDate = c.DueDate,
                    category = c.Category,
                    insurer = c.Insurer != null ? new
                    {
                        id = c.Insurer.Id,
                        firstName = c.Insurer.FirstName,
                        lastName = c.Insurer.LastName,
                        companyName = c.Insurer.CompanyName,
                        email = c.Insurer.Email
                    } : null,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = data,
                    count = data.Count
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
            try
            {
                if (claim == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                // If insurerId and providerId are provided, validate them and use the proper creation method
                if (claim.InsurerId > 0 && claim.ProviderId > 0)
                {
                    var created = await _claimService.CreateForProviderAsync(claim.InsurerId, claim.ProviderId, claim);
                    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
                }

                // Otherwise, use the basic create method (for backward compatibility)
                var result = await _claimService.CreateAsync(claim);
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
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
                    ClientFullName = request.ClientFullName,
                    ClientEmailAddress = request.ClientEmailAddress,
                    ClientPhoneNumber = request.ClientPhoneNumber,
                    ClientAddress = request.ClientAddress,
                    ClientCompany = request.ClientCompany,
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _claimService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        public class CreateClaimRequest
        {
            [Required]
            [MaxLength(50)]
            public string ClaimNumber { get; set; } = string.Empty;

            [Required]
            [MaxLength(255)]
            public string Title { get; set; } = string.Empty;

            [MaxLength(2000)]
            public string? Description { get; set; }

            [Required]
            public int ProviderId { get; set; }

            [Required]
            public int InsurerId { get; set; }

            public ClaimStatus Status { get; set; }
            public ClaimPriority Priority { get; set; }

            public decimal EstimatedAmount { get; set; }
            public decimal ApprovedAmount { get; set; }

            [MaxLength(100)]
            public string? PolicyNumber { get; set; }

            [MaxLength(100)]
            public string? PolicyHolderName { get; set; }

            [MaxLength(255)]
            public string? IncidentLocation { get; set; }

            public DateTime? IncidentDate { get; set; }
            public DateTime? DueDate { get; set; }

            [MaxLength(1000)]
            public string? Notes { get; set; }

            [MaxLength(100)]
            public string? Category { get; set; }
        }
    }
}