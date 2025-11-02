using FirebaseAdmin.Auth;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<TestController> _logger;
        private readonly IEmailService _emailService;

        public TestController(IUserService userService, ILogger<TestController> logger, IEmailService emailService)
        {
            _userService = userService;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Test endpoint to create a provider without authentication
        /// This is for testing purposes only - remove in production
        /// </summary>
        [HttpPost("create-provider")]
        public async Task<IActionResult> CreateProviderTest([FromBody] CreateProviderRequest request)
        {
            UserRecord? firebaseUser = null;
            
            try
            {
                // Step 1: Create Firebase user with Admin SDK
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
                    message = "Provider created successfully with ServiceProvider record",
                    data = new
                    {
                        userId = createdProvider.Id,
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
        /// Test endpoint to check email availability
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
        /// Send a test email via Resend/EmailService
        /// </summary>
        [HttpPost("send-email-test")]
        public async Task<IActionResult> SendEmailTest([FromBody] SendEmailTestRequest request)
        {
            try
            {
                await _emailService.SendAsync(request.To, request.Subject ?? "Test Email", request.Html ?? "<p>Hello from InsuranceClaimsAPI</p>");
                return Ok(new { success = true, message = "Email queued (best-effort)", to = request.To });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", request.To);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        public class SendEmailTestRequest
        {
            public string To { get; set; } = string.Empty;
            public string? Subject { get; set; }
            public string? Html { get; set; }
        }
    }
}
