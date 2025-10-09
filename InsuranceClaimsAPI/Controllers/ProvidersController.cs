using InsuranceClaimsAPI.Models.DTOs.Auth;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ProvidersController> _logger;

        public ProvidersController(IAuthService authService, ILogger<ProvidersController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var providers = await _authService.GetProvidersAsync();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting providers");
                return StatusCode(500, new { message = "An error occurred while retrieving providers" });
            }
        }

        [HttpPost("add-providers")]
        public async Task<IActionResult> AddProvider([FromBody] AddProviderRequestDto addProviderRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email already exists
                if (await _authService.EmailExistsAsync(addProviderRequest.Email))
                {
                    return Conflict(new { message = "Email already exists" });
                }

                // Create AddUserRequestDto with Provider role
                var addUserRequest = new AddUserRequestDto
                {
                    FirstName = addProviderRequest.FirstName,
                    LastName = addProviderRequest.LastName,
                    Email = addProviderRequest.Email,
                    Password = addProviderRequest.Password,
                    FirebaseUid = addProviderRequest.FirebaseUid,
                    PhoneNumber = addProviderRequest.PhoneNumber,
                    CompanyName = addProviderRequest.CompanyName,
                    Address = addProviderRequest.Address,
                    City = addProviderRequest.City,
                    PostalCode = addProviderRequest.PostalCode,
                    Country = addProviderRequest.Country,
                    Role = (int)UserRole.Provider, // Force Provider role
                    Status = UserStatus.Active
                };

                var result = await _authService.AddUserAsync(addUserRequest);
                return CreatedAtAction(nameof(GetProviders), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding provider with email: {Email}", addProviderRequest.Email);
                return StatusCode(500, new { message = "An error occurred while adding provider" });
            }
        }
    }

    public class AddProviderRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FirebaseUid { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }
    }
}
