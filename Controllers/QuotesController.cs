using System;
using System.Collections.Generic;
using System.Linq;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Quotes;
using InsuranceClaimsAPI.Models.DTOs.Messages;
using InsuranceClaimsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _quoteService;
        private readonly IClaimService _claimService;
        private readonly IMessageService _messageService;
        private readonly ILogger<QuotesController> _logger;
        private readonly IEmailService _emailService;

        public QuotesController(
            IQuoteService quoteService,
            IClaimService claimService,
            IMessageService messageService,
            ILogger<QuotesController> logger,
            IEmailService emailService)
        {
            _quoteService = quoteService;
            _claimService = claimService;
            _messageService = messageService;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost("{quoteId:int}/documents")]
        public async Task<IActionResult> UploadQuoteDocuments(int quoteId, [FromBody] IList<QuoteAttachmentRequest> attachments)
        {
            try
            {
                if (attachments == null || attachments.Count == 0)
                {
                    return BadRequest(new { success = false, error = "No attachments provided" });
                }

                var headerValue = Request.Headers["X-User-Id"].FirstOrDefault();
                if (!int.TryParse(headerValue, out var uploaderId) || uploaderId <= 0)
                {
                    return BadRequest(new { success = false, error = "Valid X-User-Id header is required" });
                }

                try
                {
                    var savedDocuments = await _quoteService.AddDocumentsAsync(
                        quoteId,
                        uploaderId,
                        attachments,
                        HttpContext.RequestAborted);

                    return Ok(new
                    {
                        success = true,
                        message = "Documents uploaded successfully",
                        data = savedDocuments.Select(d => new
                        {
                            id = d.Id,
                            fileName = d.FileName,
                            mimeType = d.MimeType,
                            fileSizeBytes = d.FileSizeBytes,
                            type = d.Type.ToString(),
                            url = d.FilePath,
                            createdAt = d.CreatedAt
                        }).ToList()
                    });
                }
                catch (InvalidOperationException ex)
                {
                    var errorMessage = ex.Message;
                    if (!string.IsNullOrWhiteSpace(errorMessage) && errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new { success = false, error = errorMessage });
                    }

                    return BadRequest(new { success = false, error = errorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading documents for quote {QuoteId}", quoteId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to upload documents",
                    details = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var quotes = await _quoteService.GetAllAsync();
            return Ok(quotes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var quote = await _quoteService.GetByIdAsync(id);
            if (quote == null)
            {
                return NotFound(new { message = "Quote not found" });
            }
            var response = new
            {
                quoteId = quote.QuoteId,
                claimId = quote.PolicyId,
                providerId = quote.ProviderId,
                amount = quote.Amount,
                status = quote.Status.ToString(),
                dateSubmitted = quote.DateSubmitted,
                documents = (quote.QuoteDocuments ?? new List<QuoteDocument>()).Select(d => new
                {
                    id = d.Id,
                    fileName = d.FileName,
                    mimeType = d.MimeType,
                    fileSizeBytes = d.FileSizeBytes,
                    type = d.Type.ToString(),
                    url = d.FilePath,
                    title = d.Title,
                    description = d.Description,
                    tags = d.Tags,
                    uploadedAt = d.CreatedAt
                }).ToList()
            };

            return Ok(new
            {
                success = true,
                data = response
            });
        }

        [HttpGet("claim/{claimId:int}")]
        public async Task<IActionResult> GetByClaim(int claimId)
        {
            var quotes = await _quoteService.GetByClaimAsync(claimId);

            var response = quotes.Select(q => new
            {
                quoteId = q.QuoteId,
                claimId = q.PolicyId,
                providerId = q.ProviderId,
                amount = q.Amount,
                status = q.Status.ToString(),
                dateSubmitted = q.DateSubmitted,
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
                count = response.Count,
                data = response
            });
        }

        [HttpGet("provider/{userId}")]
        public async Task<IActionResult> GetByProviderFirebaseId(string userId)
        {
            var quotes = await _quoteService.GetByProviderFirebaseIdAsync(userId);
            return Ok(quotes);
        }

        [HttpPost("/api/add/quotes")]
        // [Authorize(Policy = "RequireProviderRole")]
        public async Task<IActionResult> Submit([FromBody] Quote quote)
        {
            var created = await _quoteService.SubmitAsync(quote);
            return Ok(created);
        }

        /// <summary>
        /// Creates a new quote for a claim - Provider only
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequest request)
        {
            try
            {
                int? headerUserId = null;
                var headerValue = Request.Headers["X-User-Id"].FirstOrDefault();
                if (int.TryParse(headerValue, out var parsedUserId) && parsedUserId > 0)
                {
                    headerUserId = parsedUserId;
                }

                // Validate claim exists
                var claim = await _claimService.GetAsync(request.ClaimId);
                if (claim == null)
                {
                    return NotFound(new { success = false, error = "Claim not found" });
                }

                // Use the claim's provider as the quote provider to ensure FK integrity
                var providerId = claim.ProviderId;
                if (providerId <= 0)
                {
                    return BadRequest(new { success = false, error = "Claim has no associated provider" });
                }

                if (!headerUserId.HasValue)
                {
                    headerUserId = providerId;
                }

                // Create quote
                var quote = new Quote
                {
                    PolicyId = request.ClaimId,
                    ProviderId = providerId,
                    Amount = request.Amount,
                    Status = QuoteStatus.Submitted,
                    DateSubmitted = DateTime.UtcNow
                };

                var createdQuote = await _quoteService.SubmitAsync(
                    quote,
                    headerUserId,
                    request.Attachments,
                    HttpContext.RequestAborted);

                // Best-effort email notifications
                try
                {
                    var fullClaim = await _claimService.GetAsync(createdQuote.PolicyId);
                    var insurerEmail = fullClaim?.Insurer?.Email;
                    var providerEmail = fullClaim?.Provider?.Email;

                    if (!string.IsNullOrWhiteSpace(insurerEmail))
                    {
                        await _emailService.SendAsync(
                            insurerEmail,
                            "New quote submitted",
                            $"<p>A new quote has been submitted for claim #{fullClaim?.ClaimNumber ?? createdQuote.PolicyId.ToString()}.</p><p>Amount: <strong>{createdQuote.Amount:C}</strong></p><p>Status: {createdQuote.Status}</p>");
                    }

                    if (!string.IsNullOrWhiteSpace(providerEmail))
                    {
                        await _emailService.SendAsync(
                            providerEmail,
                            "Your quote was submitted",
                            $"<p>Your quote for claim #{fullClaim?.ClaimNumber ?? createdQuote.PolicyId.ToString()} was submitted successfully.</p><p>Amount: <strong>{createdQuote.Amount:C}</strong></p><p>Status: {createdQuote.Status}</p>");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send quote creation emails for QuoteId {QuoteId}", createdQuote.QuoteId);
                }

                return Ok(new
                {
                    success = true,
                    message = "Quote created successfully",
                    data = new
                    {
                        quoteId = createdQuote.QuoteId,
                        claimId = createdQuote.PolicyId,
                        providerId = createdQuote.ProviderId,
                        amount = createdQuote.Amount,
                        status = createdQuote.Status.ToString(),
                        dateSubmitted = createdQuote.DateSubmitted,
                        attachmentsUploaded = createdQuote.QuoteDocuments?.Count ?? 0,
                        attachments = createdQuote.QuoteDocuments?.Select(d => new
                        {
                            id = d.Id,
                            fileName = d.FileName,
                            mimeType = d.MimeType,
                            fileSizeBytes = d.FileSizeBytes,
                            type = d.Type.ToString(),
                            url = d.FilePath
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quote for claim {ClaimId}", request.ClaimId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create quote",
                    details = ex.Message
                });
            }
        }

        public class SetStatusRequest { public QuoteStatus Status { get; set; } public string? ReasonOrNotes { get; set; } }

        [HttpPut("{id:int}/status")]
        // [Authorize(Policy = "RequireInsurerRole")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest request)
        {
            await _quoteService.SetStatusAsync(id, request.Status, request.ReasonOrNotes);
            return NoContent();
        }

        [HttpPut("/api/quotes/update/status/{quoteId:int}")]
        // [Authorize(Roles = "Insurer,Admin")]
        public async Task<IActionResult> UpdateQuoteStatus(int quoteId, [FromBody] SetStatusRequest request)
        {
            var quote = await _quoteService.GetByIdAsync(quoteId);
            if (quote == null)
            {
                return NotFound(new { message = "Quote not found" });
            }

            await _quoteService.SetStatusAsync(quoteId, request.Status, request.ReasonOrNotes);
            return Ok(new { message = "Quote status updated successfully", quoteId, status = request.Status });
        }

        /// <summary>
        /// Deletes a quote by ID
        /// </summary>
        [HttpDelete("{id:int}")]
        // [Authorize(Policy = "RequireProviderRole")]
        public async Task<IActionResult> DeleteQuote(int id)
        {
            try
            {
                // Check if quote exists
                var quote = await _quoteService.GetByIdAsync(id);
                if (quote == null)
                {
                    return NotFound(new 
                    { 
                        success = false, 
                        error = "Quote not found" 
                    });
                }

                var deleted = await _quoteService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new 
                    { 
                        success = false, 
                        error = "Quote not found or could not be deleted" 
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Quote deleted successfully",
                    quoteId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote {QuoteId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete quote",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all messages for a specific quote
        /// </summary>
        [HttpGet("{quoteId:int}/messages")]
        public async Task<IActionResult> GetQuoteMessages(int quoteId)
        {
            try
            {
                // Get user ID from header
                var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault() ?? "0";
                var userId = int.Parse(userIdStr);

                var messages = await _messageService.GetByQuoteAsync(quoteId, userId);
                
                var response = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    ClaimId = m.ClaimId,
                    QuoteId = m.QuoteId,
                    SenderId = m.SenderId,
                    SenderName = $"{m.sender.FirstName} {m.sender.LastName}",
                    SenderEmail = m.sender.Email,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver != null ? $"{m.Receiver.FirstName} {m.Receiver.LastName}" : null,
                    ReceiverEmail = m.Receiver?.Email,
                    Type = m.Type,
                    Status = m.Status,
                    Content = m.Content,
                    Subject = m.Subject,
                    AttachmentUrl = m.AttachmentUrl,
                    AttachmentFileName = m.AttachmentFileName,
                    AttachmentMimeType = m.AttachmentMimeType,
                    AttachmentSizeBytes = m.AttachmentSizeBytes,
                    IsRead = m.IsRead,
                    ReadAt = m.ReadAt,
                    IsImportant = m.IsImportant,
                        ReplyToMessageId = m.ReplyToMessageId != null && int.TryParse(m.ReplyToMessageId, out var replyId) ? replyId : null,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = response,
                    count = response.Count
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for quote {QuoteId}", quoteId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to retrieve messages",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Send a message for a specific quote (between insurer and provider)
        /// </summary>
        [HttpPost("{quoteId:int}/messages")]
        public async Task<IActionResult> SendQuoteMessage(int quoteId, [FromBody] CreateQuoteMessageRequest request)
        {
            try
            {
                // Ensure the quoteId in the path matches the request
                if (request.QuoteId != quoteId)
                {
                    return BadRequest(new { success = false, error = "Quote ID in path does not match request body" });
                }

                // Get user ID from header
                var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault() ?? "0";
                var userId = int.Parse(userIdStr);

                // Get quote to determine the other party (receiver)
                var quote = await _quoteService.GetByIdAsync(quoteId);
                if (quote == null)
                {
                    return NotFound(new { success = false, error = "Quote not found" });
                }

                // Get the claim to find insurer and provider
                var claim = await _claimService.GetAsync(quote.PolicyId);
                if (claim == null)
                {
                    return NotFound(new { success = false, error = "Claim not found" });
                }

                // Determine receiver: if sender is provider, receiver is insurer, and vice versa
                int? receiverId = null;
                if (userId == claim.ProviderId)
                {
                    receiverId = claim.InsurerId;
                }
                else if (userId == claim.InsurerId)
                {
                    receiverId = claim.ProviderId;
                }
                else
                {
                    return Forbid("You do not have permission to send messages for this quote");
                }

                var message = new Message
                {
                    QuoteId = quoteId,
                    ReceiverId = receiverId,
                    Content = request.Content,
                    Subject = request.Subject,
                    Type = request.Type,
                    AttachmentUrl = request.AttachmentUrl,
                    AttachmentFileName = request.AttachmentFileName,
                    AttachmentMimeType = request.AttachmentMimeType,
                    AttachmentSizeBytes = request.AttachmentSizeBytes,
                    ReplyToMessageId = request.ReplyToMessageId?.ToString()
                };

                var createdMessage = await _messageService.SendQuoteMessageAsync(message, userId);

                // Get sender and receiver info for response
                var senderClaim = await _claimService.GetAsync(quote.PolicyId);
                
                string senderName;
                string senderEmail;
                string receiverName;
                string receiverEmail;

                if (userId == claim.ProviderId)
                {
                    senderName = senderClaim?.Provider != null 
                        ? $"{senderClaim.Provider.FirstName} {senderClaim.Provider.LastName}" 
                        : "Provider";
                    senderEmail = senderClaim?.Provider?.Email ?? "";
                    
                    receiverName = senderClaim?.Insurer != null 
                        ? $"{senderClaim.Insurer.FirstName} {senderClaim.Insurer.LastName}" 
                        : "Unknown";
                    receiverEmail = senderClaim?.Insurer?.Email ?? "";
                }
                else
                {
                    senderName = senderClaim?.Insurer != null 
                        ? $"{senderClaim.Insurer.FirstName} {senderClaim.Insurer.LastName}" 
                        : "Insurer";
                    senderEmail = senderClaim?.Insurer?.Email ?? "";
                    
                    receiverName = senderClaim?.Provider != null 
                        ? $"{senderClaim.Provider.FirstName} {senderClaim.Provider.LastName}" 
                        : "Unknown";
                    receiverEmail = senderClaim?.Provider?.Email ?? "";
                }

                var senderInfo = new { Name = senderName, Email = senderEmail };
                var receiverInfo = new { Name = receiverName, Email = receiverEmail };

                return Ok(new
                {
                    success = true,
                    message = "Message sent successfully",
                    data = new MessageResponseDto
                    {
                        Id = createdMessage.Id,
                        ClaimId = createdMessage.ClaimId,
                        QuoteId = createdMessage.QuoteId,
                        SenderId = createdMessage.SenderId,
                        SenderName = senderInfo.Name,
                        SenderEmail = senderInfo.Email,
                        ReceiverId = createdMessage.ReceiverId,
                        ReceiverName = receiverInfo.Name,
                        ReceiverEmail = receiverInfo.Email,
                        Type = createdMessage.Type,
                        Status = createdMessage.Status,
                        Content = createdMessage.Content,
                        Subject = createdMessage.Subject,
                        AttachmentUrl = createdMessage.AttachmentUrl,
                        AttachmentFileName = createdMessage.AttachmentFileName,
                        AttachmentMimeType = createdMessage.AttachmentMimeType,
                        AttachmentSizeBytes = createdMessage.AttachmentSizeBytes,
                        IsRead = createdMessage.IsRead,
                        ReadAt = createdMessage.ReadAt,
                        IsImportant = createdMessage.IsImportant,
                        ReplyToMessageId = createdMessage.ReplyToMessageId != null && int.TryParse(createdMessage.ReplyToMessageId, out var replyId) ? replyId : null,
                        CreatedAt = createdMessage.CreatedAt,
                        UpdatedAt = createdMessage.UpdatedAt
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for quote {QuoteId}", quoteId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to send message",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Generates an invoice PDF for a quote with claim details
        /// </summary>
        [HttpGet("{quoteId:int}/invoice/pdf")]
        public async Task<IActionResult> GenerateInvoicePdf(int quoteId)
        {
            try
            {
                var pdfBytes = await _quoteService.GenerateInvoicePdfAsync(quoteId);
                
                var fileName = $"Invoice_Quote_{quoteId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Quote or claim not found for quote {QuoteId}", quoteId);
                return NotFound(new 
                { 
                    success = false, 
                    error = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for quote {QuoteId}", quoteId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to generate invoice PDF",
                    details = ex.Message
                });
            }
        }
    }
}


