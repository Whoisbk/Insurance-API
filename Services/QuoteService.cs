using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IQuoteService
    {
        Task<Quote> SubmitAsync(Quote quote);
        Task SetStatusAsync(int quoteId, QuoteStatus status, string? reasonOrNotes = null);
        Task<IReadOnlyList<Quote>> GetByClaimAsync(int claimId);
        Task<IReadOnlyList<Quote>> GetAllAsync();
        Task<Quote?> GetByIdAsync(int quoteId);
        Task<IReadOnlyList<Quote>> GetByProviderFirebaseIdAsync(string firebaseUid);
        Task<bool> DeleteAsync(int quoteId);
    }

    public class QuoteService : IQuoteService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public QuoteService(
            InsuranceClaimsContext context, 
            IAuditService auditService,
            INotificationService notificationService)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<Quote> SubmitAsync(Quote quote)
        {
            quote.Status = QuoteStatus.Submitted;
            quote.DateSubmitted = DateTime.UtcNow;

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Create,
                EntityType = EntityType.Quote,
                EntityId = quote.QuoteId.ToString(),
                ActionDescription = "Quote submitted"
            });

            // Load claim with insurer to get insurer ID for notification
            var claim = await _context.Claims
                .Include(c => c.Insurer)
                .FirstOrDefaultAsync(c => c.Id == quote.PolicyId);

            if (claim != null && claim.InsurerId > 0)
            {
                // Notify the insurer that a new quote has been submitted
                await _notificationService.CreateAsync(new Notification
                {
                    UserId = claim.InsurerId,
                    QuoteId = quote.QuoteId,
                    Message = $"A new quote for ${quote.Amount:N2} has been submitted for claim {claim.ClaimNumber}",
                    DateSent = DateTime.UtcNow,
                    Status = NotificationStatus.Unread
                });
            }

            return quote;
        }

        public async Task SetStatusAsync(int quoteId, QuoteStatus status, string? reasonOrNotes = null)
        {
            var quote = await _context.Quotes
                .Include(q => q.Provider)
                .Include(q => q.Policy)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            
            if (quote == null) return;

            var oldStatus = quote.Status;
            quote.Status = status;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.QuoteStatusChange,
                EntityType = EntityType.Quote,
                EntityId = quoteId.ToString(),
                ActionDescription = $"Quote status changed from {oldStatus} to {status}"
            });

            // Notify the provider when quote status changes
            if (quote.ProviderId > 0 && oldStatus != status)
            {
                var statusMessage = status switch
                {
                    QuoteStatus.Approved => "approved",
                    QuoteStatus.Rejected => "rejected",
                    QuoteStatus.Revised => "requires revision",
                    _ => "updated"
                };

                var notificationMessage = $"Your quote for ${quote.Amount:N2} has been {statusMessage}";
                if (!string.IsNullOrEmpty(reasonOrNotes))
                {
                    notificationMessage += $": {reasonOrNotes}";
                }

                await _notificationService.CreateAsync(new Notification
                {
                    UserId = quote.ProviderId,
                    QuoteId = quote.QuoteId,
                    Message = notificationMessage,
                    DateSent = DateTime.UtcNow,
                    Status = NotificationStatus.Unread
                });
            }
        }

        public async Task<IReadOnlyList<Quote>> GetByClaimAsync(int claimId)
        {
            return await _context.Quotes
                .Where(q => q.PolicyId == claimId)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Quote>> GetAllAsync()
        {
            return await _context.Quotes
                .Include(q => q.Policy)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<Quote?> GetByIdAsync(int quoteId)
        {
            return await _context.Quotes
                .Include(q => q.Policy)
                .Include(q => q.Provider)
                .Include(q => q.QuoteDocuments)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
        }

        public async Task<IReadOnlyList<Quote>> GetByProviderFirebaseIdAsync(string firebaseUid)
        {
            var provider = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid && u.Role == UserRole.Provider);

            if (provider == null)
            {
                return new List<Quote>();
            }

            return await _context.Quotes
                .Include(q => q.Policy)
                .Include(q => q.QuoteDocuments)
                .Where(q => q.ProviderId == provider.Id)
                .OrderByDescending(q => q.DateSubmitted)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.Policy)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                return false;
            }

            // Log audit before deletion
            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Delete,
                EntityType = EntityType.Quote,
                EntityId = quoteId.ToString(),
                ActionDescription = $"Quote for ${quote.Amount:N2} deleted"
            });

            // Note: Notifications with QuoteId will be set to NULL automatically due to ON DELETE SET NULL constraint
            // QuoteDocuments will be cascade deleted

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}


