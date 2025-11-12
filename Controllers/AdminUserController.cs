using FirebaseAdmin.Auth;
using System.Security.Cryptography;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly ILogger<AdminUserController> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public AdminUserController(IUserService userService, IServiceProviderService serviceProviderService, ILogger<AdminUserController> logger, IEmailService emailService, IAuditService auditService)
        {
            _userService = userService;
            _serviceProviderService = serviceProviderService;
            _logger = logger;
            _emailService = emailService;
            _auditService = auditService;
        }

        // NOTE: Insurer and Provider CRUD endpoints have been moved to AdminController
        // to keep admin operations separate from normal user endpoints

        private static string GenerateSecurePassword(int length = 12)
        {
            if (length < 8)
            {
                length = 8;
            }

            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "@$!%*?&";

            var allChars = upper + lower + digits + special;
            var passwordChars = new char[length];
            var position = 0;

            passwordChars[position++] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            passwordChars[position++] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            passwordChars[position++] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            passwordChars[position++] = special[RandomNumberGenerator.GetInt32(special.Length)];

            while (position < length)
            {
                passwordChars[position++] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
            }

            for (var i = passwordChars.Length - 1; i > 0; i--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
                (passwordChars[i], passwordChars[swapIndex]) = (passwordChars[swapIndex], passwordChars[i]);
            }

            return new string(passwordChars);
        }

        /// <summary>
        /// Updates admin information (no password change)
        /// </summary>
        [HttpPut("admins/{id}")]
        [HttpPut("edit/{id}")]
        [HttpPut("/api/admins/edit/{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminRequest request)
        {
            try
            {
                var admin = await _userService.GetUserByIdAsync(id);
                if (admin == null || admin.Role != UserRole.Admin)
                {
                    return NotFound(new { success = false, error = "Admin not found" });
                }

                // Store old values for audit log
                var oldValues = $"FirstName: {admin.FirstName}, LastName: {admin.LastName}, Email: {admin.Email}";

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

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.User,
                    EntityId = admin.Id.ToString(),
                    ActionDescription = $"Admin updated: {admin.FirstName} {admin.LastName} (ID: {admin.Id})",
                    OldValues = oldValues,
                    NewValues = $"FirstName: {admin.FirstName}, LastName: {admin.LastName}, Email: {admin.Email}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

                // Update Firebase email if changed (with timeout, best effort)
                if (!string.IsNullOrEmpty(admin.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = admin.FirebaseUid,
                            Email = request.Email,
                            DisplayName = $"{request.FirstName} {request.LastName}"
                        };
                        
                        // Add 5-second timeout for Firebase update
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs).WaitAsync(cts.Token);
                        _logger.LogInformation("Firebase admin updated successfully: {FirebaseUid}", admin.FirebaseUid);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Firebase update timed out for admin {FirebaseUid}, but database update succeeded", admin.FirebaseUid);
                    }
                    catch (FirebaseAuthException firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Failed to update Firebase admin {FirebaseUid}, but database update succeeded", admin.FirebaseUid);
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error updating Firebase admin {FirebaseUid}", admin.FirebaseUid);
                    }
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
                        role = (int)admin.Role,
                        status = (int)admin.Status,
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
        /// Soft deletes admin (sets DeletedAt) and disables user in Firebase
        /// </summary>
        [HttpDelete("admins/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _userService.GetUserByIdAsync(id);
                if (admin == null || admin.Role != UserRole.Admin)
                {
                    return NotFound(new { success = false, error = "Admin not found" });
                }

                // Store user info for audit log before deletion
                var deletedUserInfo = $"FirstName: {admin.FirstName}, LastName: {admin.LastName}, Email: {admin.Email}";

                // Soft delete from database
                await _userService.DeleteUserAsync(id);

                // Disable user in Firebase if UID exists
                if (!string.IsNullOrEmpty(admin.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = admin.FirebaseUid,
                            Disabled = true
                        };
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                        _logger.LogInformation($"Firebase user disabled: {admin.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase disable failed (user may already be deleted): {ex.Message}");
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error disabling Firebase user {FirebaseUid}", admin.FirebaseUid);
                    }
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Delete,
                    EntityType = EntityType.User,
                    EntityId = id.ToString(),
                    ActionDescription = $"Admin deleted: {admin.FirstName} {admin.LastName} (ID: {id})",
                    OldValues = deletedUserInfo,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
}
