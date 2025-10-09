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
    }

    public class QuoteService : IQuoteService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IAuditService _auditService;

        public QuoteService(InsuranceClaimsContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
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

            return quote;
        }

        public async Task SetStatusAsync(int quoteId, QuoteStatus status, string? reasonOrNotes = null)
        {
            var quote = await _context.Quotes.FindAsync(quoteId);
            if (quote == null) return;

            quote.Status = status;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.QuoteStatusChange,
                EntityType = EntityType.Quote,
                EntityId = quoteId.ToString(),
                ActionDescription = $"Quote status set to {status}"
            });
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
    }
}


