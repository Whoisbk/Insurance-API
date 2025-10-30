using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Hubs;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IMessageService
    {
        Task<Message> SendAsync(Message message);
        Task<Message> SendQuoteMessageAsync(Message message, int userId);
        Task<IReadOnlyList<Message>> GetByClaimAsync(int claimId);
        Task<IReadOnlyList<Message>> GetByQuoteAsync(int quoteId, int userId);
    }

    public class MessageService : IMessageService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public MessageService(InsuranceClaimsContext context, IHubContext<MessageHub> hubContext, INotificationService notificationService, IEmailService emailService)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<Message> SendAsync(Message message)
        {
            // Set defaults
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.Status = MessageStatus.Sent;
            message.IsRead = false;

            // Validate required relationships exist to avoid FK violations
            var claimExists = await _context.Claims.AnyAsync(c => c.Id == message.ClaimId);
            if (!claimExists)
            {
                throw new InvalidOperationException("Claim not found");
            }

            var senderExists = await _context.Users.AnyAsync(u => u.Id == message.SenderId);
            if (!senderExists)
            {
                throw new InvalidOperationException("Sender not found");
            }

            if (message.ReceiverId.HasValue)
            {
                var receiverExists = await _context.Users.AnyAsync(u => u.Id == message.ReceiverId.Value);
                if (!receiverExists)
                {
                    throw new InvalidOperationException("Receiver not found");
                }
            }

            if (message.QuoteId.HasValue)
            {
                var quoteExists = await _context.Quotes.AnyAsync(q => q.QuoteId == message.QuoteId.Value);
                if (!quoteExists)
                {
                    throw new InvalidOperationException("Quote not found");
                }
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send to claim room with full message details
            await _hubContext.Clients.Group($"Claim_{message.ClaimId}").SendAsync("ReceiveMessage", new
            {
                MessageId = message.Id,
                ClaimId = message.ClaimId,
                QuoteId = message.QuoteId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Subject = message.Subject,
                Type = message.Type.ToString(),
                Status = message.Status.ToString(),
                AttachmentUrl = message.AttachmentUrl,
                AttachmentFileName = message.AttachmentFileName,
                IsRead = message.IsRead,
                IsImportant = message.IsImportant,
                ReplyToMessageId = message.ReplyToMessageId,
                CreatedAt = message.CreatedAt,
                Timestamp = DateTime.UtcNow
            });

            // If message is associated with a quote, also send to quote room
            if (message.QuoteId.HasValue)
            {
                await _hubContext.Clients.Group($"Quote_{message.QuoteId.Value}").SendAsync("ReceiveMessage", new
                {
                    MessageId = message.Id,
                    ClaimId = message.ClaimId,
                    QuoteId = message.QuoteId,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    Subject = message.Subject,
                    Type = message.Type.ToString(),
                    Status = message.Status.ToString(),
                    AttachmentUrl = message.AttachmentUrl,
                    AttachmentFileName = message.AttachmentFileName,
                    IsRead = message.IsRead,
                    IsImportant = message.IsImportant,
                    ReplyToMessageId = message.ReplyToMessageId,
                    CreatedAt = message.CreatedAt,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Also send to receiver's personal group for instant notification
            if (message.ReceiverId.HasValue)
            {
                await _hubContext.Clients.Group($"User_{message.ReceiverId.Value}").SendAsync("NewMessage", new
                {
                    MessageId = message.Id,
                    ClaimId = message.ClaimId,
                    QuoteId = message.QuoteId,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    Subject = message.Subject,
                    Type = message.Type.ToString(),
                    IsRead = false,
                    CreatedAt = message.CreatedAt,
                    Timestamp = DateTime.UtcNow
                });

                // Persist a notification for the receiver
                await _notificationService.CreateAsync(new Notification
                {
                    UserId = message.ReceiverId.Value,
                    QuoteId = message.QuoteId,
                    Message = !string.IsNullOrWhiteSpace(message.Subject)
                        ? $"New message: {message.Subject}"
                        : $"New message on claim #{message.ClaimId}",
                    DateSent = DateTime.UtcNow,
                    Status = NotificationStatus.Unread
                });

                // Best-effort email notification
                try
                {
                    var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == message.ReceiverId.Value);
                    if (receiver != null && !string.IsNullOrWhiteSpace(receiver.Email))
                    {
                        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "You have a new message" : message.Subject!;
                        var html = $"<p>You have a new message on claim #{message.ClaimId}{(message.QuoteId.HasValue ? $" / quote #{message.QuoteId.Value}" : string.Empty)}.</p><p><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(subject)}</p><p>{System.Net.WebUtility.HtmlEncode(message.Content)}</p>";
                        await _emailService.SendAsync(receiver.Email, subject, html);
                    }
                }
                catch
                {
                    // swallow email errors, already logged by EmailService where relevant
                }
            }

            return message;
        }

        public async Task<Message> SendQuoteMessageAsync(Message message, int userId)
        {
            // Security check: verify user has access to this quote
            var quote = await _context.Quotes
                .FirstOrDefaultAsync(q => q.QuoteId == message.QuoteId);

            if (quote == null)
            {
                throw new InvalidOperationException("Quote not found");
            }

            // Load the associated claim (PolicyId refers to Claim.Id)
            var claim = await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Insurer)
                .FirstOrDefaultAsync(c => c.Id == quote.PolicyId);

            if (claim == null)
            {
                throw new InvalidOperationException("Associated claim not found");
            }

            // Verify user is either the provider or the insurer for this quote
            if (claim.ProviderId != userId && claim.InsurerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to send messages for this quote");
            }

            // Ensure ClaimId matches the quote's PolicyId
            message.ClaimId = quote.PolicyId;
            message.SenderId = userId;

            return await SendAsync(message);
        }

        public async Task<IReadOnlyList<Message>> GetByClaimAsync(int claimId)
        {
            return await _context.Messages
                .Include(m => m.sender)
                .Include(m => m.Receiver)
                .Where(m => m.ClaimId == claimId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Message>> GetByQuoteAsync(int quoteId, int userId)
        {
            // Security check: verify user has access to this quote
            var quote = await _context.Quotes
                .Include(q => q.Provider)
                .Include(q => q.Policy)
                    .ThenInclude(p => p.Provider)
                .Include(q => q.Policy)
                    .ThenInclude(p => p.Insurer)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                throw new InvalidOperationException("Quote not found");
            }

            // Verify user is either the provider or the insurer for this quote
            var claim = quote.Policy;
            if (claim.ProviderId != userId && claim.InsurerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view messages for this quote");
            }

            return await _context.Messages
                .Include(m => m.sender)
                .Include(m => m.Receiver)
                .Where(m => m.QuoteId == quoteId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}


