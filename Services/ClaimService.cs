using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IClaimService
    {
        Task<Claim> CreateAsync(Claim claim);
        Task<Claim> CreateForProviderAsync(int insurerId, int providerId, Claim claim);
        Task<Claim?> GetAsync(int id);
        Task<IReadOnlyList<Claim>> GetForUserAsync(int userId);
        Task<IReadOnlyList<Claim>> GetForProviderAsync(int providerId);
        Task UpdateStatusAsync(int claimId, ClaimStatus status);
    }

    public class ClaimService : IClaimService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public ClaimService(InsuranceClaimsContext context, IAuditService auditService, INotificationService notificationService)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<Claim> CreateAsync(Claim claim)
        {
            claim.CreatedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Create,
                EntityType = EntityType.Claim,
                EntityId = claim.Id.ToString(),
                ActionDescription = "Claim created"
            });
            return claim;
        }

        public async Task<Claim> CreateForProviderAsync(int insurerId, int providerId, Claim claim)
        {
            // Validate that provider exists and has Provider role
            var provider = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == providerId && u.Role == UserRole.Provider);
            
            if (provider == null)
            {
                throw new InvalidOperationException($"Provider with ID {providerId} not found or is not a provider.");
            }

            // Validate that insurer exists and has Insurer role
            var insurer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == insurerId && u.Role == UserRole.Insurer);
            
            if (insurer == null)
            {
                throw new InvalidOperationException($"Insurer with ID {insurerId} not found or is not an insurer.");
            }

            // Generate unique claim number
            claim.ClaimNumber = await GenerateUniqueClaimNumberAsync();
            claim.ProviderId = providerId;
            claim.InsurerId = insurerId;
            claim.Status = ClaimStatus.Draft;
            claim.CreatedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;
            claim.ClaimSubmittedDate = DateTime.UtcNow;

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Create,
                EntityType = EntityType.Claim,
                EntityId = claim.Id.ToString(),
                ActionDescription = $"Claim created by insurer {insurerId} for provider {providerId}"
            });

            // Notify the provider that a new claim has been created for them
            await _notificationService.CreateAsync(new Notification
            {
                UserId = providerId,
                Message = $"A new claim '{claim.Title}' (Claim #{claim.ClaimNumber}) has been created for you by {insurer.CompanyName ?? $"{insurer.FirstName} {insurer.LastName}"}",
                DateSent = DateTime.UtcNow,
                Status = NotificationStatus.Unread
            });

            return claim;
        }

        private async Task<string> GenerateUniqueClaimNumberAsync()
        {
            string claimNumber;
            bool isUnique = false;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                // Generate claim number: CLM-YYYYMMDD-HHMMSS-Random
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var random = new Random().Next(1000, 9999);
                claimNumber = $"CLM-{timestamp}-{random}";

                isUnique = !await _context.Claims.AnyAsync(c => c.ClaimNumber == claimNumber);
                attempts++;

                if (attempts >= maxAttempts)
                {
                    throw new InvalidOperationException("Failed to generate unique claim number after multiple attempts.");
                }
            } while (!isUnique);

            return claimNumber;
        }

        public async Task<Claim?> GetAsync(int id)
        {
            return await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Insurer)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IReadOnlyList<Claim>> GetForUserAsync(int userId)
        {
            return await _context.Claims
                .Where(c => c.ProviderId == userId || c.InsurerId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Claim>> GetForProviderAsync(int providerId)
        {
            return await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Insurer)
                .Where(c => c.ProviderId == providerId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(int claimId, ClaimStatus status)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return;

            var old = claim.Status;
            claim.Status = status;
            claim.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.ClaimStatusChange,
                EntityType = EntityType.Claim,
                EntityId = claimId.ToString(),
                ActionDescription = $"Claim status changed from {old} to {status}"
            });
        }
    }
}


