using InsuranceClaimsAPI.Models.DTOs.Admin;
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
        private readonly IServiceProviderService _serviceProviderService;
        private readonly ILogger<ProvidersController> _logger;

        public ProvidersController(IAuthService authService, IServiceProviderService serviceProviderService, ILogger<ProvidersController> logger)
        {
            _authService = authService;
            _serviceProviderService = serviceProviderService;
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

        /// <summary>
        /// Gets all ServiceProvider records
        /// </summary>
        [HttpGet("service-providers")]
        public async Task<IActionResult> GetServiceProviders()
        {
            try
            {
                var serviceProviders = await _serviceProviderService.GetAllServiceProvidersAsync();
                return Ok(new
                {
                    success = true,
                    count = serviceProviders.Count,
                    data = serviceProviders.Select(sp => new
                    {
                        providerId = sp.ProviderId,
                        userId = sp.UserId,
                        name = sp.Name,
                        specialization = sp.Specialization,
                        phoneNumber = sp.PhoneNumber,
                        email = sp.Email,
                        address = sp.Address,
                        endDate = sp.EndDate,
                        user = new
                        {
                            id = sp.User.Id,
                            firstName = sp.User.FirstName,
                            lastName = sp.User.LastName,
                            email = sp.User.Email
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting service providers");
                return StatusCode(500, new { message = "An error occurred while retrieving service providers" });
            }
        }

        /// <summary>
        /// Gets a specific ServiceProvider by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceProvider(int id)
        {
            try
            {
                var serviceProvider = await _serviceProviderService.GetServiceProviderByIdAsync(id);
                if (serviceProvider == null)
                {
                    return NotFound(new { success = false, error = "Service provider not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        providerId = serviceProvider.ProviderId,
                        userId = serviceProvider.UserId,
                        name = serviceProvider.Name,
                        specialization = serviceProvider.Specialization,
                        phoneNumber = serviceProvider.PhoneNumber,
                        email = serviceProvider.Email,
                        address = serviceProvider.Address,
                        endDate = serviceProvider.EndDate,
                        user = new
                        {
                            id = serviceProvider.User.Id,
                            firstName = serviceProvider.User.FirstName,
                            lastName = serviceProvider.User.LastName,
                            email = serviceProvider.User.Email
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting service provider with ID: {ProviderId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving service provider" });
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

        /// <summary>
        /// Updates a service provider by ID
        /// </summary>
        [HttpPut("{id}")]
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateServiceProviderRequest request)
        {
            try
            {
                var serviceProvider = await _serviceProviderService.GetServiceProviderByIdAsync(id);
                if (serviceProvider == null)
                {
                    return NotFound(new { success = false, error = "Service provider not found" });
                }

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    serviceProvider.Name = request.Name;
                }
                
                if (!string.IsNullOrWhiteSpace(request.Specialization))
                {
                    serviceProvider.Specialization = request.Specialization;
                }
                
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    serviceProvider.PhoneNumber = request.PhoneNumber;
                }
                
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    serviceProvider.Email = request.Email;
                }
                
                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    serviceProvider.Address = request.Address;
                }
                
                if (request.EndDate.HasValue)
                {
                    serviceProvider.EndDate = request.EndDate.Value;
                }

                await _serviceProviderService.UpdateServiceProviderAsync(serviceProvider);

                return Ok(new
                {
                    success = true,
                    message = "Service provider updated successfully",
                    data = new
                    {
                        providerId = serviceProvider.ProviderId,
                        userId = serviceProvider.UserId,
                        name = serviceProvider.Name,
                        specialization = serviceProvider.Specialization,
                        phoneNumber = serviceProvider.PhoneNumber,
                        email = serviceProvider.Email,
                        address = serviceProvider.Address,
                        endDate = serviceProvider.EndDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service provider with ID: {ProviderId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update service provider",
                    details = ex.Message
                });
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
