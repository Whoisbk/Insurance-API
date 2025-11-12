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
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProvidersController> _logger;

        public ProvidersController(IAuthService authService, IServiceProviderService serviceProviderService, IUserService userService, IAuditService auditService, ILogger<ProvidersController> logger)
        {
            _authService = authService;
            _serviceProviderService = serviceProviderService;
            _userService = userService;
            _auditService = auditService;
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
                        insurerId = sp.InsurerId,
                        insurer = sp.Insurer != null ? new
                        {
                            insurerId = sp.Insurer.InsurerId,
                            name = sp.Insurer.Name,
                            email = sp.Insurer.Email
                        } : null,
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
                        insurerId = serviceProvider.InsurerId,
                        insurer = serviceProvider.Insurer != null ? new
                        {
                            insurerId = serviceProvider.Insurer.InsurerId,
                            name = serviceProvider.Insurer.Name,
                            email = serviceProvider.Insurer.Email
                        } : null,
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
        /// Updates a provider User by ID (for admin editing provider users)
        /// </summary>
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> UpdateProviderUser(int id, [FromBody] UpdateProviderRequest request)
        {
            try
            {
                var provider = await _userService.GetUserByIdAsync(id);
                if (provider == null || provider.Role != UserRole.Provider)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

                // Store old values for audit log
                var oldValues = $"FirstName: {provider.FirstName}, LastName: {provider.LastName}, Email: {provider.Email}, CompanyName: {provider.CompanyName}";

                // Update database
                provider.FirstName = request.FirstName;
                provider.LastName = request.LastName;
                provider.Email = request.Email;
                provider.CompanyName = request.CompanyName;
                provider.PhoneNumber = request.PhoneNumber;
                provider.Address = request.Address;
                provider.City = request.City;
                provider.PostalCode = request.PostalCode;
                provider.Country = request.Country;

                await _userService.UpdateUserAsync(provider);

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.User,
                    EntityId = provider.Id.ToString(),
                    ActionDescription = $"Provider updated: {provider.FirstName} {provider.LastName} (ID: {provider.Id})",
                    OldValues = oldValues,
                    NewValues = $"FirstName: {provider.FirstName}, LastName: {provider.LastName}, Email: {provider.Email}, CompanyName: {provider.CompanyName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

                return Ok(new
                {
                    success = true,
                    message = "Provider updated successfully",
                    data = provider
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update provider",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates a service provider by ID
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateServiceProviderRequest request)
        {
            try
            {
                var serviceProvider = await _serviceProviderService.GetServiceProviderByIdAsync(id);
                if (serviceProvider == null)
                {
                    return NotFound(new { success = false, error = "Service provider not found" });
                }

                // Update User fields if provided
                if (serviceProvider.User != null)
                {
                    bool userUpdated = false;

                    if (!string.IsNullOrWhiteSpace(request.FirstName))
                    {
                        serviceProvider.User.FirstName = request.FirstName;
                        userUpdated = true;
                    }

                    if (!string.IsNullOrWhiteSpace(request.LastName))
                    {
                        serviceProvider.User.LastName = request.LastName;
                        userUpdated = true;
                    }

                    if (!string.IsNullOrWhiteSpace(request.UserEmail))
                    {
                        // Check if email already exists for another user
                        if (await _userService.EmailExistsForAnotherUserAsync(serviceProvider.User.Id, request.UserEmail))
                        {
                            return Conflict(new { success = false, error = "Email already exists for another user" });
                        }
                        serviceProvider.User.Email = request.UserEmail;
                        userUpdated = true;
                    }

                    // Update the user if any user fields were changed
                    if (userUpdated)
                    {
                        await _userService.UpdateUserAsync(serviceProvider.User);
                    }
                }

                // Update ServiceProvider fields
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
                
                if (request.InsurerId.HasValue)
                {
                    serviceProvider.InsurerId = request.InsurerId.Value;
                }

                if (request.EndDate.HasValue)
                {
                    serviceProvider.EndDate = request.EndDate.Value;
                }

                await _serviceProviderService.UpdateServiceProviderAsync(serviceProvider);

                // Reload the entity to get the latest data including navigation properties
                var updatedServiceProvider = await _serviceProviderService.GetServiceProviderByIdAsync(id);

                return Ok(new
                {
                    success = true,
                    message = "Service provider updated successfully",
                    data = new
                    {
                        providerId = updatedServiceProvider.ProviderId,
                        userId = updatedServiceProvider.UserId,
                        name = updatedServiceProvider.Name,
                        specialization = updatedServiceProvider.Specialization,
                        phoneNumber = updatedServiceProvider.PhoneNumber,
                        email = updatedServiceProvider.Email,
                        address = updatedServiceProvider.Address,
                        endDate = updatedServiceProvider.EndDate,
                        insurerId = updatedServiceProvider.InsurerId,
                        insurer = updatedServiceProvider.Insurer != null ? new
                        {
                            insurerId = updatedServiceProvider.Insurer.InsurerId,
                            name = updatedServiceProvider.Insurer.Name,
                            email = updatedServiceProvider.Insurer.Email
                        } : null,
                        user = new
                        {
                            id = updatedServiceProvider.User.Id,
                            firstName = updatedServiceProvider.User.FirstName,
                            lastName = updatedServiceProvider.User.LastName,
                            email = updatedServiceProvider.User.Email
                        }
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

        /// <summary>
        /// Gets the current user ID from request headers or claims
        /// </summary>
        private int? GetCurrentUserId()
        {
            // Try to get user ID from header (X-User-Id)
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                return userId;
            }

            // Try to get user ID from claims (if using authentication)
            var userIdClaim = User?.FindFirst("userId")?.Value ?? User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userIdFromClaim))
            {
                return userIdFromClaim;
            }

            // If no user ID found, return null (system action)
            return null;
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
