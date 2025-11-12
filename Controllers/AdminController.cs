using FirebaseAdmin.Auth;
using System.Security.Cryptography;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Controllers
{
    /// <summary>
    /// Admin-only controller for managing insurers and providers
    /// Separate from normal user endpoints to keep admin operations isolated
    /// All endpoints follow the pattern: /api/AdminUser/...
    /// </summary>
    [ApiController]
    [Route("api/AdminUser")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IServiceProviderService _serviceProviderService;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly InsuranceClaimsContext _context;
        private readonly IQuoteService _quoteService;

        public AdminController(
            IUserService userService,
            IServiceProviderService serviceProviderService,
            ILogger<AdminController> logger,
            IEmailService emailService,
            IAuditService auditService,
            InsuranceClaimsContext context,
            IQuoteService quoteService)
        {
            _userService = userService;
            _serviceProviderService = serviceProviderService;
            _logger = logger;
            _emailService = emailService;
            _auditService = auditService;
            _context = context;
            _quoteService = quoteService;
        }

        #region Insurer Management

        /// <summary>
        /// Creates a new insurer with Firebase authentication
        /// </summary>
        [HttpPost("insurers")]
        public async Task<IActionResult> CreateInsurer([FromBody] CreateInsurerRequest request)
        {
            UserRecord? firebaseUser = null;

            try
            {
                // Validate email doesn't already exist
                if (await _userService.EmailExistsAsync(request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
                }

                var generatedPassword = GenerateSecurePassword();

                // Step 1: Create Firebase user with Admin SDK
                _logger.LogInformation($"Creating Firebase account for insurer: {request.Email}");

                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = generatedPassword,
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

                string? verificationLink = null;
                try
                {
                    verificationLink = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(request.Email);
                }
                catch (Exception linkEx)
                {
                    _logger.LogWarning(linkEx, "Failed to generate email verification link for insurer {Email}", request.Email);
                }

                // Step 3: Save to database
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

                // Best-effort welcome email
                try
                {
                    var verificationHtml = verificationLink != null
                        ? $"<p>Please verify your email by <a href=\"{verificationLink}\">clicking here</a>.</p>"
                        : "<p>We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.</p>";

                    var verificationText = verificationLink != null
                        ? $"Please verify your email by visiting: {verificationLink}"
                        : "We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.";

                    var htmlBody = $"""
                        <p>Hi {createdInsurer.FirstName},</p>
                        <p>Your insurer account has been created successfully.</p>
                        <p><strong>Temporary Password:</strong> {generatedPassword}</p>
                        <p>Please log in using this password and update it after signing in.</p>
                        {verificationHtml}
                        """;

                    var textBody =
                        $"Hi {createdInsurer.FirstName},\n\nYour insurer account has been created successfully.\nTemporary password: {generatedPassword}\nPlease change it after signing in.\n{verificationText}";

                    await _emailService.SendAsync(
                        createdInsurer.Email,
                        "Welcome to Insurance Claims Portal",
                        htmlBody,
                        textBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send welcome email to insurer {Email}", createdInsurer.Email);
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Create,
                    EntityType = EntityType.User,
                    EntityId = createdInsurer.Id.ToString(),
                    ActionDescription = $"Insurer created: {createdInsurer.FirstName} {createdInsurer.LastName} (ID: {createdInsurer.Id})",
                    NewValues = $"FirstName: {createdInsurer.FirstName}, LastName: {createdInsurer.LastName}, Email: {createdInsurer.Email}, CompanyName: {createdInsurer.CompanyName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
                        role = (int)createdInsurer.Role,
                        status = (int)createdInsurer.Status,
                        firebaseUid = createdInsurer.FirebaseUid,
                        createdAt = createdInsurer.CreatedAt
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
        /// Updates insurer information
        /// </summary>
        [HttpPut("insurers/{id}")]
        public async Task<IActionResult> UpdateInsurer(int id, [FromBody] UpdateInsurerRequest request)
        {
            try
            {
                var insurer = await _userService.GetUserByIdAsync(id);
                if (insurer == null || insurer.Role != UserRole.Insurer)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                // Check if email is being changed and if it already exists for another user
                if (insurer.Email.ToLower() != request.Email.ToLower() &&
                    await _userService.EmailExistsForAnotherUserAsync(id, request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
                }

                // Store old values for audit log
                var oldValues = $"FirstName: {insurer.FirstName}, LastName: {insurer.LastName}, Email: {insurer.Email}, CompanyName: {insurer.CompanyName}";

                // Update User entity
                insurer.FirstName = request.FirstName;
                insurer.LastName = request.LastName;
                insurer.Email = request.Email;
                insurer.CompanyName = request.CompanyName;
                insurer.PhoneNumber = request.PhoneNumber;
                insurer.Address = request.Address;
                insurer.City = request.City;
                insurer.PostalCode = request.PostalCode;
                insurer.Country = request.Country;
                insurer.UpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(insurer);

                // Update Insurer entity
                var insurerProfile = await _context.Insurers
                    .FirstOrDefaultAsync(i => i.UserId == id);

                if (insurerProfile != null)
                {
                    insurerProfile.Name = $"{request.FirstName} {request.LastName}".Trim();
                    insurerProfile.Email = request.Email;
                    insurerProfile.PhoneNumber = request.PhoneNumber;
                    insurerProfile.Address = request.Address;
                    insurerProfile.UpdatedAt = DateTime.UtcNow;
                    _context.Insurers.Update(insurerProfile);
                    await _context.SaveChangesAsync();
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.User,
                    EntityId = insurer.Id.ToString(),
                    ActionDescription = $"Insurer updated: {insurer.FirstName} {insurer.LastName} (ID: {insurer.Id})",
                    OldValues = oldValues,
                    NewValues = $"FirstName: {insurer.FirstName}, LastName: {insurer.LastName}, Email: {insurer.Email}, CompanyName: {insurer.CompanyName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

                // Update Firebase email if changed (with timeout, best effort)
                if (!string.IsNullOrEmpty(insurer.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = insurer.FirebaseUid,
                            Email = request.Email,
                            DisplayName = $"{request.FirstName} {request.LastName}"
                        };

                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs).WaitAsync(cts.Token);
                        _logger.LogInformation("Firebase insurer updated successfully: {FirebaseUid}", insurer.FirebaseUid);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Firebase update timed out for insurer {FirebaseUid}, but database update succeeded", insurer.FirebaseUid);
                    }
                    catch (FirebaseAuthException firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Failed to update Firebase insurer {FirebaseUid}, but database update succeeded", insurer.FirebaseUid);
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error updating Firebase insurer {FirebaseUid}", insurer.FirebaseUid);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Insurer updated successfully",
                    data = new
                    {
                        id = insurer.Id,
                        firstName = insurer.FirstName,
                        lastName = insurer.LastName,
                        email = insurer.Email,
                        companyName = insurer.CompanyName,
                        phoneNumber = insurer.PhoneNumber,
                        address = insurer.Address,
                        city = insurer.City,
                        postalCode = insurer.PostalCode,
                        country = insurer.Country,
                        role = (int)insurer.Role,
                        status = (int)insurer.Status,
                        firebaseUid = insurer.FirebaseUid,
                        updatedAt = insurer.UpdatedAt
                    }
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
        /// Soft deletes insurer (sets DeletedAt) and disables user in Firebase
        /// </summary>
        [HttpDelete("insurers/{id}")]
        public async Task<IActionResult> DeleteInsurer(int id)
        {
            try
            {
                var insurer = await _userService.GetUserByIdAsync(id);
                if (insurer == null || insurer.Role != UserRole.Insurer)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                // Store user info for audit log before deletion
                var deletedUserInfo = $"FirstName: {insurer.FirstName}, LastName: {insurer.LastName}, Email: {insurer.Email}, CompanyName: {insurer.CompanyName}";

                // Soft delete from database
                await _userService.DeleteUserAsync(id);

                // Disable user in Firebase if UID exists
                if (!string.IsNullOrEmpty(insurer.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = insurer.FirebaseUid,
                            Disabled = true
                        };
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                        _logger.LogInformation($"Firebase user disabled: {insurer.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase disable failed (user may already be deleted): {ex.Message}");
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error disabling Firebase user {FirebaseUid}", insurer.FirebaseUid);
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
                    ActionDescription = $"Insurer deleted: {insurer.FirstName} {insurer.LastName} (ID: {id})",
                    OldValues = deletedUserInfo,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
        /// Gets a specific insurer with related claims, notifications, and quotes
        /// </summary>
        [HttpGet("insurers/{id:int}")]
        public async Task<IActionResult> GetInsurer(int id)
        {
            try
            {
                var insurer = await _userService.GetInsurerByIdWithDetailsAsync(id);

                if (insurer == null)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                // Get all quotes for this insurer
                var quotes = await _quoteService.GetForInsurerAsync(id);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = insurer.Id,
                        firstName = insurer.FirstName,
                        lastName = insurer.LastName,
                        email = insurer.Email,
                        companyName = insurer.CompanyName,
                        phoneNumber = insurer.PhoneNumber,
                        address = insurer.Address,
                        city = insurer.City,
                        postalCode = insurer.PostalCode,
                        country = insurer.Country,
                        role = (int)insurer.Role,
                        status = (int)insurer.Status,
                        firebaseUid = insurer.FirebaseUid,
                        createdAt = insurer.CreatedAt,
                        updatedAt = insurer.UpdatedAt,
                        claims = insurer.ManagedClaims.Select(c => new
                        {
                            id = c.Id,
                            claimNumber = c.ClaimNumber,
                            title = c.Title,
                            clientFullName = c.ClientFullName,
                            clientEmailAddress = c.ClientEmailAddress,
                            clientPhoneNumber = c.ClientPhoneNumber,
                            clientAddress = c.ClientAddress,
                            clientCompany = c.ClientCompany,
                            status = (int)c.Status,
                            priority = (int)c.Priority,
                            provider = c.Provider != null ? new
                            {
                                id = c.Provider.Id,
                                firstName = c.Provider.FirstName,
                                lastName = c.Provider.LastName,
                                companyName = c.Provider.CompanyName
                            } : null,
                            estimatedAmount = c.EstimatedAmount,
                            approvedAmount = c.ApprovedAmount,
                            policyNumber = c.PolicyNumber,
                            policyHolderName = c.PolicyHolderName,
                            createdAt = c.CreatedAt,
                            updatedAt = c.UpdatedAt,
                            quotes = c.Quotes.Select(q => new
                            {
                                id = q.QuoteId,
                                amount = q.Amount,
                                status = (int)q.Status,
                                dateSubmitted = q.DateSubmitted
                            })
                        }),
                        quotes = quotes.Select(q => new
                        {
                            quoteId = q.QuoteId,
                            claimId = q.PolicyId,
                            amount = q.Amount,
                            status = q.Status.ToString(),
                            dateSubmitted = q.DateSubmitted,
                            provider = q.Policy?.Provider != null ? new
                            {
                                id = q.Policy.Provider.Id,
                                firstName = q.Policy.Provider.FirstName,
                                lastName = q.Policy.Provider.LastName,
                                companyName = q.Policy.Provider.CompanyName,
                                email = q.Policy.Provider.Email
                            } : null,
                            claim = q.Policy != null ? new
                            {
                                id = q.Policy.Id,
                                claimNumber = q.Policy.ClaimNumber,
                                title = q.Policy.Title,
                                status = q.Policy.Status.ToString()
                            } : null,
                            documents = (q.QuoteDocuments ?? new List<QuoteDocument>()).Select(d => new
                            {
                                id = d.Id,
                                fileName = d.FileName,
                                mimeType = d.MimeType,
                                fileSizeBytes = d.FileSizeBytes,
                                type = d.Type.ToString(),
                                url = d.FilePath,
                                uploadedAt = d.CreatedAt
                            }).ToList()
                        }).ToList(),
                        notifications = insurer.Notifications.Select(n => new
                        {
                            id = n.NotificationId,
                            quoteId = n.QuoteId,
                            message = n.Message,
                            dateSent = n.DateSent,
                            status = n.Status.ToString()
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting insurer {id}: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all insurers with both user data and insurer table data
        /// </summary>
        [HttpGet("insurers")]
        public async Task<IActionResult> GetInsurers()
        {
            try
            {
                var insurers = await _context.Users
                    .Where(u => u.Role == UserRole.Insurer && u.DeletedAt == null)
                    .Include(u => u.InsurerProfile)
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = insurers.Select(i => new
                    {
                        // User data
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
                        role = (int)i.Role,
                        status = (int)i.Status,
                        firebaseUid = i.FirebaseUid,
                        createdAt = i.CreatedAt,
                        updatedAt = i.UpdatedAt,
                        // Insurer table data
                        insurer = i.InsurerProfile != null ? new
                        {
                            insurerId = i.InsurerProfile.InsurerId,
                            userId = i.InsurerProfile.UserId,
                            name = i.InsurerProfile.Name,
                            email = i.InsurerProfile.Email,
                            phoneNumber = i.InsurerProfile.PhoneNumber,
                            address = i.InsurerProfile.Address,
                            createdAt = i.InsurerProfile.CreatedAt,
                            updatedAt = i.InsurerProfile.UpdatedAt
                        } : null
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

        #endregion

        #region Provider Management

        /// <summary>
        /// Creates a new provider with Firebase authentication
        /// </summary>
        [HttpPost("providers")]
        public async Task<IActionResult> CreateProvider([FromBody] CreateProviderRequest request)
        {
            UserRecord? firebaseUser = null;

            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, error = "Invalid request data", details = ModelState });
                }

                // Validate terms acceptance if provided
                if (request.AcceptTerms.HasValue && !request.AcceptTerms.Value)
                {
                    return BadRequest(new { success = false, error = "Terms and conditions must be accepted" });
                }

                // Validate email doesn't already exist
                if (await _userService.EmailExistsAsync(request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
                }

                // Validate insurer exists
                var insurerExists = await _context.Insurers
                    .AnyAsync(i => i.InsurerId == request.InsurerId);
                
                if (!insurerExists)
                {
                    return BadRequest(new { success = false, error = "Invalid InsurerId. Insurer not found." });
                }

                var generatedPassword = GenerateSecurePassword();

                // Step 1: Create Firebase user with Admin SDK
                _logger.LogInformation($"Creating Firebase account for provider: {request.Email}");

                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = generatedPassword,
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

                string? verificationLink = null;
                try
                {
                    verificationLink = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(request.Email);
                }
                catch (Exception linkEx)
                {
                    _logger.LogWarning(linkEx, "Failed to generate email verification link for provider {Email}", request.Email);
                }

                // Step 3: Save to database
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
                var serviceProvider = await _serviceProviderService.GetServiceProviderByUserIdAsync(createdProvider.Id);

                // Best-effort welcome email
                try
                {
                    var verificationHtml = verificationLink != null
                        ? $"<p>Please verify your email by <a href=\"{verificationLink}\">clicking here</a>.</p>"
                        : "<p>We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.</p>";

                    var verificationText = verificationLink != null
                        ? $"Please verify your email by visiting: {verificationLink}"
                        : "We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.";

                    var htmlBody = $"""
                        <p>Hi {createdProvider.FirstName},</p>
                        <p>Your provider account has been created successfully.</p>
                        <p><strong>Temporary Password:</strong> {generatedPassword}</p>
                        <p>Please log in using this password and update it after signing in.</p>
                        {verificationHtml}
                        """;

                    var textBody =
                        $"Hi {createdProvider.FirstName},\n\nYour provider account has been created successfully.\nTemporary password: {generatedPassword}\nPlease change it after signing in.\n{verificationText}";

                    await _emailService.SendAsync(
                        createdProvider.Email,
                        "Welcome to Insurance Claims Portal",
                        htmlBody,
                        textBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send welcome email to provider {Email}", createdProvider.Email);
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Create,
                    EntityType = EntityType.User,
                    EntityId = createdProvider.Id.ToString(),
                    ActionDescription = $"Provider created: {createdProvider.FirstName} {createdProvider.LastName} (ID: {createdProvider.Id})",
                    NewValues = $"FirstName: {createdProvider.FirstName}, LastName: {createdProvider.LastName}, Email: {createdProvider.Email}, CompanyName: {createdProvider.CompanyName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
                        role = (int)createdProvider.Role,
                        status = (int)createdProvider.Status,
                        firebaseUid = createdProvider.FirebaseUid,
                        createdAt = createdProvider.CreatedAt,
                        insurerId = serviceProvider?.InsurerId
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
        /// Updates provider information
        /// Supports both /api/AdminUser/providers/{id} and /api/providers/edit/{id} routes
        /// </summary>
        [HttpPut("providers/{id}")]
        [HttpPut("providers/edit/{id}")]
        [HttpPut("/api/providers/edit/{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] UpdateProviderRequest request)
        {
            try
            {
                var provider = await _userService.GetUserByIdAsync(id);
                if (provider == null || provider.Role != UserRole.Provider)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

                // Check if email is being changed and if it already exists for another user
                if (provider.Email.ToLower() != request.Email.ToLower() &&
                    await _userService.EmailExistsForAnotherUserAsync(id, request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
                }

                // Store old values for audit log
                var oldValues = $"FirstName: {provider.FirstName}, LastName: {provider.LastName}, Email: {provider.Email}, CompanyName: {provider.CompanyName}";

                // Update User entity
                provider.FirstName = request.FirstName;
                provider.LastName = request.LastName;
                provider.Email = request.Email;
                provider.CompanyName = request.CompanyName;
                provider.PhoneNumber = request.PhoneNumber;
                provider.Address = request.Address;
                provider.City = request.City;
                provider.PostalCode = request.PostalCode;
                provider.Country = request.Country;
                provider.UpdatedAt = DateTime.UtcNow;

                await _userService.UpdateUserAsync(provider);

                // Update ServiceProvider entity
                var serviceProvider = await _serviceProviderService.GetServiceProviderByUserIdAsync(id);
                if (serviceProvider != null)
                {
                    serviceProvider.Name = $"{request.FirstName} {request.LastName}".Trim();
                    serviceProvider.Email = request.Email;
                    serviceProvider.PhoneNumber = request.PhoneNumber ?? "";
                    serviceProvider.Address = request.Address ?? "";
                    await _serviceProviderService.UpdateServiceProviderAsync(serviceProvider);
                }

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

                // Update Firebase email if changed (with timeout, best effort)
                if (!string.IsNullOrEmpty(provider.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = provider.FirebaseUid,
                            Email = request.Email,
                            DisplayName = $"{request.FirstName} {request.LastName}"
                        };

                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs).WaitAsync(cts.Token);
                        _logger.LogInformation("Firebase provider updated successfully: {FirebaseUid}", provider.FirebaseUid);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Firebase update timed out for provider {FirebaseUid}, but database update succeeded", provider.FirebaseUid);
                    }
                    catch (FirebaseAuthException firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Failed to update Firebase provider {FirebaseUid}, but database update succeeded", provider.FirebaseUid);
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error updating Firebase provider {FirebaseUid}", provider.FirebaseUid);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Provider updated successfully",
                    data = new
                    {
                        id = provider.Id,
                        firstName = provider.FirstName,
                        lastName = provider.LastName,
                        email = provider.Email,
                        companyName = provider.CompanyName,
                        phoneNumber = provider.PhoneNumber,
                        address = provider.Address,
                        city = provider.City,
                        postalCode = provider.PostalCode,
                        country = provider.Country,
                        role = (int)provider.Role,
                        status = (int)provider.Status,
                        firebaseUid = provider.FirebaseUid,
                        updatedAt = provider.UpdatedAt
                    }
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
        /// Soft deletes provider (sets DeletedAt) and disables user in Firebase
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

                // Store user info for audit log before deletion
                var deletedUserInfo = $"FirstName: {provider.FirstName}, LastName: {provider.LastName}, Email: {provider.Email}, CompanyName: {provider.CompanyName}";

                // Soft delete from database
                await _userService.DeleteUserAsync(id);

                // Disable user in Firebase if UID exists
                if (!string.IsNullOrEmpty(provider.FirebaseUid))
                {
                    try
                    {
                        var updateArgs = new UserRecordArgs
                        {
                            Uid = provider.FirebaseUid,
                            Disabled = true
                        };
                        await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                        _logger.LogInformation($"Firebase user disabled: {provider.FirebaseUid}");
                    }
                    catch (FirebaseAuthException ex)
                    {
                        _logger.LogWarning($"Firebase disable failed (user may already be deleted): {ex.Message}");
                    }
                    catch (Exception firebaseEx)
                    {
                        _logger.LogWarning(firebaseEx, "Unexpected error disabling Firebase user {FirebaseUid}", provider.FirebaseUid);
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
                    ActionDescription = $"Provider deleted: {provider.FirstName} {provider.LastName} (ID: {id})",
                    OldValues = deletedUserInfo,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
                        role = (int)p.Role,
                        status = (int)p.Status,
                        firebaseUid = p.FirebaseUid,
                        createdAt = p.CreatedAt,
                        updatedAt = p.UpdatedAt,
                        quotes = p.Quotes.Select(q => new
                        {
                            id = q.QuoteId,
                            policyId = q.PolicyId,
                            amount = q.Amount,
                            status = (int)q.Status,
                            dateSubmitted = q.DateSubmitted
                        }),
                        notifications = p.Notifications.Select(n => new
                        {
                            id = n.NotificationId,
                            quoteId = n.QuoteId,
                            message = n.Message,
                            dateSent = n.DateSent,
                            status = n.Status.ToString()
                        })
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
        /// Gets a single provider by ID
        /// </summary>
        [HttpGet("providers/{id}")]
        public async Task<IActionResult> GetProvider(int id)
        {
            try
            {
                var provider = await _context.Users
                    .Where(u => u.Id == id && u.Role == UserRole.Provider && u.DeletedAt == null)
                    .Include(u => u.Quotes)
                    .Include(u => u.Notifications)
                    .Include(u => u.ServiceProviderProfile)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    return NotFound(new { success = false, error = "Provider not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = provider.Id,
                        firstName = provider.FirstName,
                        lastName = provider.LastName,
                        email = provider.Email,
                        companyName = provider.CompanyName,
                        phoneNumber = provider.PhoneNumber,
                        address = provider.Address,
                        city = provider.City,
                        postalCode = provider.PostalCode,
                        country = provider.Country,
                        role = (int)provider.Role,
                        status = (int)provider.Status,
                        firebaseUid = provider.FirebaseUid,
                        createdAt = provider.CreatedAt,
                        updatedAt = provider.UpdatedAt,
                        insurerId = provider.ServiceProviderProfile?.InsurerId,
                        quotes = provider.Quotes.Select(q => new
                        {
                            id = q.QuoteId,
                            policyId = q.PolicyId,
                            amount = q.Amount,
                            status = (int)q.Status,
                            dateSubmitted = q.DateSubmitted
                        }),
                        notifications = provider.Notifications.Select(n => new
                        {
                            id = n.NotificationId,
                            quoteId = n.QuoteId,
                            message = n.Message,
                            dateSent = n.DateSent,
                            status = n.Status.ToString()
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting provider: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve provider",
                    details = ex.Message
                });
            }
        }

        #endregion

        #region Admin Management

        /// <summary>
        /// Creates a new admin with Firebase authentication
        /// </summary>
        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            UserRecord? firebaseUser = null;

            try
            {
                // Validate email doesn't already exist
                if (await _userService.EmailExistsAsync(request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
                }

                var generatedPassword = GenerateSecurePassword();

                // Step 1: Create Firebase user with Admin SDK
                _logger.LogInformation($"Creating Firebase account for admin: {request.Email}");

                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = generatedPassword,
                    EmailVerified = false,
                    Disabled = false,
                    DisplayName = $"{request.FirstName} {request.LastName}"
                };

                firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
                _logger.LogInformation($"Firebase user created with UID: {firebaseUser.Uid}");

                // Step 2: Set custom claims for role-based access (Admin role)
                var claims = new Dictionary<string, object>
                {
                    { "role", "admin" },
                    { "roleId", 3 }
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);

                string? verificationLink = null;
                try
                {
                    verificationLink = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(request.Email);
                }
                catch (Exception linkEx)
                {
                    _logger.LogWarning(linkEx, "Failed to generate email verification link for admin {Email}", request.Email);
                }

                // Step 3: Save to database as Admin (Role = 3)
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

                // Best-effort welcome email
                try
                {
                    var verificationHtml = verificationLink != null
                        ? $"<p>Please verify your email by <a href=\"{verificationLink}\">clicking here</a>.</p>"
                        : "<p>We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.</p>";

                    var verificationText = verificationLink != null
                        ? $"Please verify your email by visiting: {verificationLink}"
                        : "We couldn't generate your verification link automatically. Please contact support if you need assistance verifying your email.";

                    var htmlBody = $"""
                        <p>Hi {createdAdmin.FirstName},</p>
                        <p>Your admin account has been created successfully.</p>
                        <p><strong>Temporary Password:</strong> {generatedPassword}</p>
                        <p>Please log in using this password and update it after signing in.</p>
                        {verificationHtml}
                        """;

                    var textBody =
                        $"Hi {createdAdmin.FirstName},\n\nYour admin account has been created successfully.\nTemporary password: {generatedPassword}\nPlease change it after signing in.\n{verificationText}";

                    await _emailService.SendAsync(
                        createdAdmin.Email,
                        "Welcome to Insurance Claims Portal",
                        htmlBody,
                        textBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send welcome email to admin {Email}", createdAdmin.Email);
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Create,
                    EntityType = EntityType.User,
                    EntityId = createdAdmin.Id.ToString(),
                    ActionDescription = $"Admin created: {createdAdmin.FirstName} {createdAdmin.LastName} (ID: {createdAdmin.Id})",
                    NewValues = $"FirstName: {createdAdmin.FirstName}, LastName: {createdAdmin.LastName}, Email: {createdAdmin.Email}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

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
                        role = (int)createdAdmin.Role,
                        status = (int)createdAdmin.Status,
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
        /// Gets all admins (Role = 3, Admins)
        /// </summary>
        [HttpGet("admins")]
        [HttpGet("users")]
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
                        role = (int)a.Role,
                        status = (int)a.Status,
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
        /// Updates admin information
        /// </summary>
        [HttpPut("admins/{id}")]
        [HttpPut("edit/{id}")]
        [HttpPut("/api/admins/edit/{id}")]
        public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateAdminRequest request)
        {
            try
            {
                var admin = await _userService.GetUserByIdAsync(id);
                if (admin == null || admin.Role != UserRole.Admin)
                {
                    return NotFound(new { success = false, error = "Admin not found" });
                }

                // Check if email is being changed and if it already exists for another user
                if (admin.Email.ToLower() != request.Email.ToLower() &&
                    await _userService.EmailExistsForAnotherUserAsync(id, request.Email))
                {
                    return BadRequest(new { success = false, error = "An account with this email already exists" });
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

        #endregion

        #region Utility Endpoints

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
        /// Deletes a Firebase user by Firebase UID
        /// </summary>
        [HttpDelete("firebase/{firebaseUid}")]
        public async Task<IActionResult> DeleteFirebaseUser(string firebaseUid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firebaseUid))
                {
                    return BadRequest(new { success = false, error = "Firebase UID is required" });
                }

                // Check if user exists in database
                var user = await _userService.GetUserByFirebaseUidAsync(firebaseUid);
                if (user == null)
                {
                    return NotFound(new { success = false, error = "User not found in database" });
                }

                // Delete from Firebase
                try
                {
                    await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUid);
                    _logger.LogInformation($"Firebase user deleted: {firebaseUid}");
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogWarning($"Firebase delete failed: {ex.Message}");
                    return BadRequest(new { success = false, error = $"Failed to delete Firebase user: {ex.Message}" });
                }

                // Log audit entry
                var currentUserId = GetCurrentUserId();
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = currentUserId,
                    Action = AuditAction.Delete,
                    EntityType = EntityType.User,
                    EntityId = user.Id.ToString(),
                    ActionDescription = $"Firebase user deleted: {user.FirstName} {user.LastName} (Firebase UID: {firebaseUid})",
                    OldValues = $"FirebaseUid: {firebaseUid}, Email: {user.Email}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

                return Ok(new
                {
                    success = true,
                    message = "Firebase user deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting Firebase user: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete Firebase user",
                    details = ex.Message
                });
            }
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}

