using System.Collections.Generic;
using System.Linq;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/insurers")]
    public class InsurersController : ControllerBase
    {
        private readonly IQuoteService _quoteService;
        private readonly IClaimService _claimService;
        private readonly InsuranceClaimsContext _context;
        private readonly ILogger<InsurersController> _logger;

        public InsurersController(
            IQuoteService quoteService,
            IClaimService claimService,
            InsuranceClaimsContext context,
            ILogger<InsurersController> logger)
        {
            _quoteService = quoteService;
            _claimService = claimService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all quotes related to claims owned by the specified insurer.
        /// </summary>
        [HttpGet("{insurerId:int}/quotes")]
        public async Task<IActionResult> GetQuotesForInsurer(int insurerId)
        {
            try
            {
                var insurerExists = await _context.Users
                    .AnyAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);

                if (!insurerExists)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                var quotes = await _quoteService.GetForInsurerAsync(insurerId);

                var data = quotes.Select(q => new
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
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotes for insurer {InsurerId}", insurerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve quotes for insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all claims owned by the specified insurer.
        /// </summary>
        [HttpGet("{insurerId:int}/claims")]
        public async Task<IActionResult> GetClaimsForInsurer(int insurerId)
        {
            try
            {
                var insurerExists = await _context.Users
                    .AnyAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);

                if (!insurerExists)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                var claims = await _claimService.GetForInsurerAsync(insurerId);

                var data = claims.Select(c => new
                {
                    claimId = c.Id,
                    claimNumber = c.ClaimNumber,
                    title = c.Title,
                    clientFullName = c.ClientFullName,
                    clientEmailAddress = c.ClientEmailAddress,
                    clientPhoneNumber = c.ClientPhoneNumber,
                    clientAddress = c.ClientAddress,
                    clientCompany = c.ClientCompany,
                    status = c.Status.ToString(),
                    priority = c.Priority.ToString(),
                    estimatedAmount = c.EstimatedAmount,
                    approvedAmount = c.ApprovedAmount,
                    incidentDate = c.IncidentDate,
                    dueDate = c.DueDate,
                    provider = c.Provider != null ? new
                    {
                        id = c.Provider.Id,
                        firstName = c.Provider.FirstName,
                        lastName = c.Provider.LastName,
                        companyName = c.Provider.CompanyName,
                        email = c.Provider.Email
                    } : null,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving claims for insurer {InsurerId}", insurerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve claims for insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all notifications that belong to the specified insurer.
        /// </summary>
        [HttpGet("{insurerId:int}/notifications")]
        public async Task<IActionResult> GetNotificationsForInsurer(int insurerId)
        {
            try
            {
                var insurerExists = await _context.Users
                    .AnyAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);

                if (!insurerExists)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                var notifications = await _context.Notifications
                    .Include(n => n.Quote)
                        .ThenInclude(q => q.Policy)
                    .Where(n => n.UserId == insurerId)
                    .OrderByDescending(n => n.DateSent)
                    .ToListAsync();

                var data = notifications.Select(n => new
                {
                    notificationId = n.NotificationId,
                    userId = n.UserId,
                    message = n.Message,
                    dateSent = n.DateSent,
                    status = n.Status.ToString(),
                    quote = n.Quote != null ? new
                    {
                        quoteId = n.Quote.QuoteId,
                        amount = n.Quote.Amount,
                        status = n.Quote.Status.ToString(),
                        claimId = n.Quote.PolicyId
                    } : null
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for insurer {InsurerId}", insurerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve notifications for insurer",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets all providers associated with the specified insurer via claims.
        /// </summary>
        [HttpGet("{insurerId:int}/providers")]
        public async Task<IActionResult> GetProvidersForInsurer(int insurerId)
        {
            try
            {
                var insurerExists = await _context.Users
                    .AnyAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);

                if (!insurerExists)
                {
                    return NotFound(new { success = false, error = "Insurer not found" });
                }

                var providers = await _claimService.GetProvidersForInsurerAsync(insurerId);

                var data = providers.Select(p => new
                {
                    id = p.Id,
                    firstName = p.FirstName,
                    lastName = p.LastName,
                    companyName = p.CompanyName,
                    email = p.Email,
                    phoneNumber = p.PhoneNumber,
                    city = p.City,
                    country = p.Country,
                    status = p.Status.ToString()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving providers for insurer {InsurerId}", insurerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve providers for insurer",
                    details = ex.Message
                });
            }
        }
    }
}

