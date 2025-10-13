using FirebaseAdmin.Auth;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using InsuranceClaimsAPI.Models.DTOs.Auth;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IAuthService authService, IUserService userService, ILogger<UsersController> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("firebase/{firebaseUid}")]
        public async Task<IActionResult> GetUserByFirebaseUid(string firebaseUid)
        {
            try
            {
                var user = await _authService.GetUserByFirebaseUidAsync(firebaseUid);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by Firebase UID: {FirebaseUid}", firebaseUid);
                return StatusCode(500, new { message = "An error occurred while retrieving user" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInsurers()
        {
            try
            {
                var users = await _authService.GetInsurersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting insurers");
                return StatusCode(500, new { message = "An error occurred while retrieving insurers" });
            }
        }

        /// <summary>
        /// Updates a user (admin/insurer) by ID
        /// </summary>
        [HttpPut("{id}")]
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminRequest request)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, error = "User not found" });
                }

                // Update database
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.PhoneNumber = request.PhoneNumber;
                user.Address = request.Address;
                user.City = request.City;
                user.PostalCode = request.PostalCode;
                user.Country = request.Country;
                user.UpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(user);

                // Update Firebase email if changed (with timeout, best effort)
                if (!string.IsNullOrEmpty(user.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = user.FirebaseUid,
                            Email = request.Email,
                            DisplayName = $"{request.FirstName} {request.LastName}"
                        };
                        
                        // Add 5-second timeout for Firebase update
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs).WaitAsync(cts.Token);
                        _logger.LogInformation("Firebase user updated successfully: {FirebaseUid}", user.FirebaseUid);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Firebase update timed out for user {FirebaseUid}, but database update succeeded", user.FirebaseUid);
                    }
                    catch (FirebaseAuthException firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Failed to update Firebase user {FirebaseUid}, but database update succeeded", user.FirebaseUid);
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error updating Firebase user {FirebaseUid}", user.FirebaseUid);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully",
                    data = new
                    {
                        id = user.Id,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        address = user.Address,
                        city = user.City,
                        postalCode = user.PostalCode,
                        country = user.Country,
                        role = user.Role.ToString(),
                        status = user.Status.ToString(),
                        firebaseUid = user.FirebaseUid,
                        updatedAt = user.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update user",
                    details = ex.Message
                });
            }
        }
    }
}
