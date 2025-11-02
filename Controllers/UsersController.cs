using System;
using FirebaseAdmin.Auth;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.User;
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
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Request body is required"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, error = "User not found" });
                }

                var updateResult = await TryApplyUserProfileUpdates(user, request);
                if (updateResult.ErrorResult != null)
                {
                    return updateResult.ErrorResult;
                }

                if (!updateResult.HasUpdates)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "No fields provided to update"
                    });
                }

                await _userService.UpdateUserAsync(user);

                // Update Firebase email if changed (with timeout, best effort)
                if (updateResult.EmailChanged && !string.IsNullOrEmpty(user.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = user.FirebaseUid,
                            Email = user.Email,
                            DisplayName = $"{user.FirstName} {user.LastName}".Trim()
                        };

                        // Add 5-second timeout for Firebase update
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs).WaitAsync(cts.Token);
                        _logger.LogInformation("Firebase user updated successfully: {FirebaseUid}", user.FirebaseUid);
                    }
                    catch (OperationCanceledException ocEx)
                    {
                        _logger.LogWarning(ocEx, "Firebase update timed out for user {FirebaseUid}, but database update succeeded", user.FirebaseUid);
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
                        companyName = user.CompanyName,
                        address = user.Address,
                        city = user.City,
                        postalCode = user.PostalCode,
                        country = user.Country,
                        profileImageUrl = user.ProfileImageUrl,
                        role = (int)user.Role,
                        status = (int)user.Status,
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

        private async Task<(bool HasUpdates, bool EmailChanged, IActionResult? ErrorResult)> TryApplyUserProfileUpdates(Models.Domain.User user, UpdateUserProfileRequest request)
        {
            var (requiredHasUpdates, requiredError) = ApplyRequiredFields(user, request);
            if (requiredError != null)
            {
                return (false, false, requiredError);
            }

            var (emailHasUpdates, emailChanged, emailError) = await ApplyEmailUpdateAsync(user, request);
            if (emailError != null)
            {
                return (false, false, emailError);
            }

            var optionalHasUpdates = ApplyOptionalFields(user, request);

            var hasUpdates = requiredHasUpdates || emailHasUpdates || optionalHasUpdates;

            return (hasUpdates, emailChanged, null);
        }

        private (bool HasUpdates, IActionResult? ErrorResult) ApplyRequiredFields(Models.Domain.User user, UpdateUserProfileRequest request)
        {
            var hasUpdates = false;

            var requiredFields = new[]
            {
                new RequiredStringField(request.FirstName, value => user.FirstName = value, "First name"),
                new RequiredStringField(request.LastName, value => user.LastName = value, "Last name")
            };

            foreach (var field in requiredFields)
            {
                if (field.Value is null)
                {
                    continue;
                }

                hasUpdates = true;
                var trimmed = field.Value.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    return (false, BadRequest(new { success = false, error = $"{field.FieldName} cannot be empty" }));
                }

                field.Setter(trimmed);
            }

            return (hasUpdates, null);
        }

        private async Task<(bool HasUpdates, bool EmailChanged, IActionResult? ErrorResult)> ApplyEmailUpdateAsync(Models.Domain.User user, UpdateUserProfileRequest request)
        {
            if (request.Email == null)
            {
                return (false, false, null);
            }

            var email = request.Email.Trim();
            if (string.IsNullOrEmpty(email))
            {
                return (false, false, BadRequest(new { success = false, error = "Email cannot be empty" }));
            }

            if (email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return (true, false, null);
            }

            var emailInUse = await _userService.EmailExistsForAnotherUserAsync(user.Id, email);
            if (emailInUse)
            {
                return (true, false, Conflict(new { success = false, error = "Email already exists" }));
            }

            user.Email = email;
            return (true, true, null);
        }

        private static bool ApplyOptionalFields(Models.Domain.User user, UpdateUserProfileRequest request)
        {
            var hasUpdates = false;

            var optionalFields = new[]
            {
                new OptionalStringField(request.PhoneNumber, value => user.PhoneNumber = value),
                new OptionalStringField(request.CompanyName, value => user.CompanyName = value),
                new OptionalStringField(request.Address, value => user.Address = value),
                new OptionalStringField(request.City, value => user.City = value),
                new OptionalStringField(request.PostalCode, value => user.PostalCode = value),
                new OptionalStringField(request.Country, value => user.Country = value),
                new OptionalStringField(request.ProfileImageUrl, value => user.ProfileImageUrl = value)
            };

            foreach (var field in optionalFields)
            {
                if (field.Value is null)
                {
                    continue;
                }

                hasUpdates = true;
                var trimmed = field.Value.Trim();
                field.Setter(string.IsNullOrWhiteSpace(trimmed) ? null : trimmed);
            }

            return hasUpdates;
        }

        private sealed record RequiredStringField(string? Value, Action<string> Setter, string FieldName);

        private sealed record OptionalStringField(string? Value, Action<string?> Setter);
    }
}
