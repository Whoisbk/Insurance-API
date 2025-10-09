using FirebaseAdmin.Auth;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(IUserService userService, ILogger<AdminUserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new insurer with Firebase authentication
        /// Admin stays logged in because we use Firebase Admin SDK
        /// </summary>
        [HttpPost("insurers")]
        [Authorize(Roles = "Insurer")]
        public async Task<IActionResult> CreateInsurer([FromBody] CreateInsurerRequest request)
        {
            UserRecord? firebaseUser = null;
            
            try
            {
                // Step 1: Create Firebase user with Admin SDK (doesn't log anyone in/out)
                _logger.LogInformation($"Creating Firebase account for insurer: {request.Email}");
                
                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    EmailVerified = false,
                    Disabled = false,
                    DisplayName = $"{request.FirstName} {request.LastName}"
                };

                firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
                _logger.LogInformation($"Firebase user created with UID: {firebaseUser.Uid}");

                // Step 2: Set custom claims for role-based access
                var claims = new Dictionary<string, object>
                {
                    { "role", "insurer" },
                    { "roleId", 1 }
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);

                // Step 3: Save to your database
                var insurer = new User
                {
                    FirebaseUid = firebaseUser.Uid,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    CompanyName = request.CompanyName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    Role = UserRole.Insurer,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdInsurer = await _userService.CreateInsurerAsync(insurer);

                return Ok(new
                {
                    success = true,
                    message = "Insurer created successfully",
                    data = new
                    {
                        id = createdInsurer.Id,
                        firstName = createdInsurer.FirstName,
                        lastName = createdInsurer.LastName,
                        email = createdInsurer.Email,
                        companyName = createdInsurer.CompanyName,
                        phoneNumber = createdInsurer.PhoneNumber,
                        address = createdInsurer.Address,
                        city = createdInsurer.City,
                        postalCode = createdInsurer.PostalCode,
                        country = createdInsurer.Country,
                        role = createdInsurer.Role.ToString(),
                        status = createdInsurer.Status.ToString(),
                        firebaseUid = createdInsurer.FirebaseUid,
                        createdAt = createdInsurer.CreatedAt
                    }
                });
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError($"Firebase error: {ex.Message}");
                
                // Handle specific Firebase errors
                var errorMessage = ex.AuthErrorCode switch
                {
                    AuthErrorCode.EmailAlreadyExists => "An account with this email already exists",
                    _ => $"Firebase authentication error: {ex.Message}"
                };

                return BadRequest(new { success = false, error = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating insurer: {ex.Message}");

                // If database save failed but Firebase user was created, clean up
                if (firebaseUser != null)
                {
                    try
                    {
                        _logger.LogWarning($"Rolling back Firebase user creation: {firebaseUser.Uid}");
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUser.Uid);
                        _logger.LogInformation("Firebase user cleanup successful");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError($"Failed to cleanup Firebase user: {cleanupEx.Message}");
                        _logger.LogError($"MANUAL CLEANUP REQUIRED - Firebase UID: {firebaseUser.Uid}");
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a new provider with Firebase authentication
        /// Admin stays logged in because we use Firebase Admin SDK
        /// </summary>
        [HttpPost("providers")]
        public async Task<IActionResult> CreateProvider([FromBody] CreateProviderRequest request)
        {
            UserRecord? firebaseUser = null;
            
            try
            {
                // Step 1: Create Firebase user with Admin SDK (doesn't log anyone in/out)
                _logger.LogInformation($"Creating Firebase account for provider: {request.Email}");
                
                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    EmailVerified = false,
                    Disabled = false,
                    DisplayName = $"{request.FirstName} {request.LastName}"
                };

                firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
                _logger.LogInformation($"Firebase user created with UID: {firebaseUser.Uid}");

                // Step 2: Set custom claims for role-based access
                var claims = new Dictionary<string, object>
                {
                    { "role", "provider" },
                    { "roleId", 2 }
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);

                // Step 3: Save to your database
                var provider = new User
                {
                    FirebaseUid = firebaseUser.Uid,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    CompanyName = request.CompanyName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    Role = UserRole.Provider,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdProvider = await _userService.CreateProviderWithServiceProviderAsync(provider, request);

                return Ok(new
                {
                    success = true,
                    message = "Provider created successfully",
                    data = new
                    {
                        id = createdProvider.Id,
                        firstName = createdProvider.FirstName,
                        lastName = createdProvider.LastName,
                        email = createdProvider.Email,
                        companyName = createdProvider.CompanyName,
                        phoneNumber = createdProvider.PhoneNumber,
                        address = createdProvider.Address,
                        city = createdProvider.City,
                        postalCode = createdProvider.PostalCode,
                        country = createdProvider.Country,
                        role = createdProvider.Role.ToString(),
                        status = createdProvider.Status.ToString(),
                        firebaseUid = createdProvider.FirebaseUid,
                        createdAt = createdProvider.CreatedAt
                    }
                });
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError($"Firebase error: {ex.Message}");
                
                // Handle specific Firebase errors
                var errorMessage = ex.AuthErrorCode switch
                {
                    AuthErrorCode.EmailAlreadyExists => "An account with this email already exists",
                    _ => $"Firebase authentication error: {ex.Message}"
                };

                return BadRequest(new { success = false, error = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating provider: {ex.Message}");

                // If database save failed but Firebase user was created, clean up
                if (firebaseUser != null)
                {
                    try
                    {
                        _logger.LogWarning($"Rolling back Firebase user creation: {firebaseUser.Uid}");
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUser.Uid);
                        _logger.LogInformation("Firebase user cleanup successful");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError($"Failed to cleanup Firebase user: {cleanupEx.Message}");
                        _logger.LogError($"MANUAL CLEANUP REQUIRED - Firebase UID: {firebaseUser.Uid}");
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create provider",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates insurer information (no password change)
        /// </summary>
        [HttpPut("insurers/{id}")]
        [Authorize(Roles = "Insurer")]
        public async Task<IActionResult> UpdateInsurer(int id, [FromBody] UpdateInsurerRequest request)
        {
            try
            {
                var insurer = await _userService.GetUserByIdAsync(id);
                if (insurer == null || insurer.Role != UserRole.Insurer)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                // Update database
                insurer.FirstName = request.FirstName;
                insurer.LastName = request.LastName;
                insurer.Email = request.Email;
                insurer.CompanyName = request.CompanyName;
                insurer.PhoneNumber = request.PhoneNumber;
                insurer.Address = request.Address;
                insurer.City = request.City;
                insurer.PostalCode = request.PostalCode;
                insurer.Country = request.Country;

                await _userService.UpdateUserAsync(insurer);

                // Update Firebase email if changed
                if (insurer.FirebaseUid != null && request.Email != insurer.Email)
                {
                    var updateArgs = new UserRecordArgs
                    {
                        Uid = insurer.FirebaseUid,
                        Email = request.Email,
                        DisplayName = $"{request.FirstName} {request.LastName}"
                    };
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                }

                return Ok(new
                {
                    success = true,
                    message = "Insurer updated successfully",
                    data = insurer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating insurer: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates provider information (no password change)
        /// </summary>
        [HttpPut("providers/{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateProviderRequest request)
        {
            try
            {
                var provider = await _userService.GetUserByIdAsync(id);
                if (provider == null || provider.Role != UserRole.Provider)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

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

                // Update Firebase email if changed
                if (provider.FirebaseUid != null && request.Email != provider.Email)
                {
                    var updateArgs = new UserRecordArgs
                    {
                        Uid = provider.FirebaseUid,
                        Email = request.Email,
                        DisplayName = $"{request.FirstName} {request.LastName}"
                    };
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                }

                return Ok(new
                {
                    success = true,
                    message = "Provider updated successfully",
                    data = provider
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating provider: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update provider",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Deletes insurer from both database and Firebase
        /// </summary>
        [HttpDelete("insurers/{id}")]
        [Authorize(Roles = "Insurer")]
        public async Task<IActionResult> DeleteInsurer(int id)
        {
            try
            {
                var insurer = await _userService.GetUserByIdAsync(id);
                if (insurer == null || insurer.Role != UserRole.Insurer)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                // Delete from database first
                await _userService.DeleteUserAsync(id);

                // Delete from Firebase if UID exists
                if (!string.IsNullOrEmpty(insurer.FirebaseUid))
                {
                    try
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(insurer.FirebaseUid);
                        _logger.LogInformation($"Firebase user deleted: {insurer.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase deletion failed (user may already be deleted): {ex.Message}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Insurer deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting insurer: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Deletes provider from both database and Firebase
        /// </summary>
        [HttpDelete("providers/{id}")]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            try
            {
                var provider = await _userService.GetUserByIdAsync(id);
                if (provider == null || provider.Role != UserRole.Provider)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

                // Delete from database first
                await _userService.DeleteUserAsync(id);

                // Delete from Firebase if UID exists
                if (!string.IsNullOrEmpty(provider.FirebaseUid))
                {
                    try
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(provider.FirebaseUid);
                        _logger.LogInformation($"Firebase user deleted: {provider.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase deletion failed (user may already be deleted): {ex.Message}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Provider deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting provider: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete provider",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all insurers
        /// </summary>
        [HttpGet("insurers")]
        [Authorize(Roles = "Insurer")]
        public async Task<IActionResult> GetInsurers()
        {
            try
            {
                var insurers = await _userService.GetUsersByRoleAsync(UserRole.Insurer);
                return Ok(new
                {
                    success = true,
                    data = insurers.Select(i => new
                    {
                        id = i.Id,
                        firstName = i.FirstName,
                        lastName = i.LastName,
                        email = i.Email,
                        companyName = i.CompanyName,
                        phoneNumber = i.PhoneNumber,
                        address = i.Address,
                        city = i.City,
                        postalCode = i.PostalCode,
                        country = i.Country,
                        role = i.Role.ToString(),
                        status = i.Status.ToString(),
                        firebaseUid = i.FirebaseUid,
                        createdAt = i.CreatedAt,
                        updatedAt = i.UpdatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting insurers: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve insurers",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var providers = await _userService.GetUsersByRoleAsync(UserRole.Provider);
                return Ok(new
                {
                    success = true,
                    data = providers.Select(p => new
                    {
                        id = p.Id,
                        firstName = p.FirstName,
                        lastName = p.LastName,
                        email = p.Email,
                        companyName = p.CompanyName,
                        phoneNumber = p.PhoneNumber,
                        address = p.Address,
                        city = p.City,
                        postalCode = p.PostalCode,
                        country = p.Country,
                        role = p.Role.ToString(),
                        status = p.Status.ToString(),
                        firebaseUid = p.FirebaseUid,
                        createdAt = p.CreatedAt,
                        updatedAt = p.UpdatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting providers: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve providers",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Check if email exists (no authentication required for frontend compatibility)
        /// </summary>
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            try
            {
                var exists = await _userService.EmailExistsAsync(email);
                return Ok(new
                {
                    success = true,
                    email = email,
                    exists = exists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking email: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to check email",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a new admin with Firebase authentication
        /// </summary>
        [HttpPost("admins")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            UserRecord? firebaseUser = null;
            
            try
            {
                // Step 1: Create Firebase user with Admin SDK
                _logger.LogInformation($"Creating Firebase account for admin: {request.Email}");
                
                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    EmailVerified = false,
                    Disabled = false,
                    DisplayName = $"{request.FirstName} {request.LastName}"
                };

                firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
                _logger.LogInformation($"Firebase user created with UID: {firebaseUser.Uid}");

                // Step 2: Set custom claims for role-based access
                var claims = new Dictionary<string, object>
                {
                    { "role", "admin" },
                    { "roleId", 3 }
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);

                // Step 3: Save to your database
                var admin = new User
                {
                    FirebaseUid = firebaseUser.Uid,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    Role = UserRole.Admin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdAdmin = await _userService.CreateAdminAsync(admin);

                return Ok(new
                {
                    success = true,
                    message = "Admin created successfully",
                    data = new
                    {
                        id = createdAdmin.Id,
                        firstName = createdAdmin.FirstName,
                        lastName = createdAdmin.LastName,
                        email = createdAdmin.Email,
                        phoneNumber = createdAdmin.PhoneNumber,
                        address = createdAdmin.Address,
                        city = createdAdmin.City,
                        postalCode = createdAdmin.PostalCode,
                        country = createdAdmin.Country,
                        role = createdAdmin.Role.ToString(),
                        status = createdAdmin.Status.ToString(),
                        firebaseUid = createdAdmin.FirebaseUid,
                        createdAt = createdAdmin.CreatedAt
                    }
                });
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError($"Firebase error: {ex.Message}");
                
                var errorMessage = ex.AuthErrorCode switch
                {
                    AuthErrorCode.EmailAlreadyExists => "An account with this email already exists",
                    _ => $"Firebase authentication error: {ex.Message}"
                };

                return BadRequest(new { success = false, error = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating admin: {ex.Message}");

                // If database save failed but Firebase user was created, clean up
                if (firebaseUser != null)
                {
                    try
                    {
                        _logger.LogWarning($"Rolling back Firebase user creation: {firebaseUser.Uid}");
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUser.Uid);
                        _logger.LogInformation("Firebase user cleanup successful");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError($"Failed to cleanup Firebase user: {cleanupEx.Message}");
                        _logger.LogError($"MANUAL CLEANUP REQUIRED - Firebase UID: {firebaseUser.Uid}");
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create admin",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates admin information (no password change)
        /// </summary>
        [HttpPut("admins/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminRequest request)
        {
            try
            {
                var admin = await _userService.GetUserByIdAsync(id);
                if (admin == null || admin.Role != UserRole.Admin)
                {
                    return NotFound(new { success = false, error = "Admin not found" });
                }

                // Update database
                admin.FirstName = request.FirstName;
                admin.LastName = request.LastName;
                admin.Email = request.Email;
                admin.PhoneNumber = request.PhoneNumber;
                admin.Address = request.Address;
                admin.City = request.City;
                admin.PostalCode = request.PostalCode;
                admin.Country = request.Country;
                admin.UpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(admin);

                // Update Firebase email if changed
                if (admin.FirebaseUid != null)
                {
                    var updateArgs = new UserRecordArgs
                    {
                        Uid = admin.FirebaseUid,
                        Email = request.Email,
                        DisplayName = $"{request.FirstName} {request.LastName}"
                    };
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                }

                return Ok(new
                {
                    success = true,
                    message = "Admin updated successfully",
                    data = new
                    {
                        id = admin.Id,
                        firstName = admin.FirstName,
                        lastName = admin.LastName,
                        email = admin.Email,
                        phoneNumber = admin.PhoneNumber,
                        address = admin.Address,
                        city = admin.City,
                        postalCode = admin.PostalCode,
                        country = admin.Country,
                        role = admin.Role.ToString(),
                        status = admin.Status.ToString(),
                        firebaseUid = admin.FirebaseUid,
                        updatedAt = admin.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating admin: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update admin",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all admins
        /// </summary>
        [HttpGet("admins")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _userService.GetUsersByRoleAsync(UserRole.Admin);
                return Ok(new
                {
                    success = true,
                    data = admins.Select(a => new
                    {
                        id = a.Id,
                        firstName = a.FirstName,
                        lastName = a.LastName,
                        email = a.Email,
                        phoneNumber = a.PhoneNumber,
                        address = a.Address,
                        city = a.City,
                        postalCode = a.PostalCode,
                        country = a.Country,
                        role = a.Role.ToString(),
                        status = a.Status.ToString(),
                        firebaseUid = a.FirebaseUid,
                        createdAt = a.CreatedAt,
                        updatedAt = a.UpdatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting admins: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve admins",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Deletes admin from both database and Firebase
        /// </summary>
        [HttpDelete("admins/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _userService.GetUserByIdAsync(id);
                if (admin == null || admin.Role != UserRole.Admin)
                {
                    return NotFound(new { success = false, error = "Admin not found" });
                }

                // Delete from database first
                await _userService.DeleteUserAsync(id);

                // Delete from Firebase if UID exists
                if (!string.IsNullOrEmpty(admin.FirebaseUid))
                {
                    try
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(admin.FirebaseUid);
                        _logger.LogInformation($"Firebase user deleted: {admin.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase deletion failed (user may already be deleted): {ex.Message}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Admin deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting admin: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete admin",
                    details = ex.Message
                });
            }
        }
    }
}
