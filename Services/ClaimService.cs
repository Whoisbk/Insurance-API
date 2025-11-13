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
        Task<IReadOnlyList<Claim>> GetAllAsync();
        Task<IReadOnlyList<Claim>> GetForUserAsync(int userId);
        Task<IReadOnlyList<Claim>> GetForProviderAsync(int providerId);
        Task<IReadOnlyList<Claim>> GetForInsurerAsync(int insurerId);
        Task<bool> DeleteAsync(int claimId);
        Task UpdateStatusAsync(int claimId, ClaimStatus status);
        Task<Claim?> UpdateAsync(int id, Claim claim);
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
            // Auto-generate claim number if not provided
            if (string.IsNullOrWhiteSpace(claim.ClaimNumber))
            {
                claim.ClaimNumber = await GenerateUniqueClaimNumberAsync();
            }

            // Set default status if not provided
            if (claim.Status == 0)
            {
                claim.Status = ClaimStatus.Draft;
            }

            claim.CreatedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;
            
            if (claim.ClaimSubmittedDate == null && claim.Status == ClaimStatus.Submitted)
            {
                claim.ClaimSubmittedDate = DateTime.UtcNow;
            }

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

        public async Task<bool> DeleteAsync(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.ClaimDocuments)
                .Include(c => c.Messages)
                .Include(c => c.Quotes)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null)
            {
                return false;
            }

            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Delete,
                EntityType = EntityType.Claim,
                EntityId = claimId.ToString(),
                ActionDescription = "Claim deleted"
            });

            return true;
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

        public async Task<IReadOnlyList<Claim>> GetAllAsync()
        {
            return await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Insurer)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
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

        public async Task<IReadOnlyList<Claim>> GetForInsurerAsync(int insurerId)
        {
            return await _context.Claims
                .Include(c => c.Provider)
                .Include(c => c.Insurer)
                .Where(c => c.InsurerId == insurerId)
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

        public async Task<Claim?> UpdateAsync(int id, Claim updatedClaim)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return null;

            // Update only the fields that should be updatable
            // Preserve: Id, ClaimNumber, CreatedAt, ProviderId, InsurerId (unless explicitly changed)
            claim.Title = updatedClaim.Title;
            claim.Description = updatedClaim.Description;
            claim.Status = updatedClaim.Status;
            claim.Priority = updatedClaim.Priority;
            claim.EstimatedAmount = updatedClaim.EstimatedAmount;
            claim.ApprovedAmount = updatedClaim.ApprovedAmount;
            claim.PolicyNumber = updatedClaim.PolicyNumber;
            claim.PolicyHolderName = updatedClaim.PolicyHolderName;
            claim.ClientFullName = updatedClaim.ClientFullName;
            claim.ClientEmailAddress = updatedClaim.ClientEmailAddress;
            claim.ClientPhoneNumber = updatedClaim.ClientPhoneNumber;
            claim.ClientAddress = updatedClaim.ClientAddress;
            claim.ClientCompany = updatedClaim.ClientCompany;
            claim.IncidentLocation = updatedClaim.IncidentLocation;
            claim.IncidentDate = updatedClaim.IncidentDate;
            claim.DueDate = updatedClaim.DueDate;
            claim.Notes = updatedClaim.Notes;
            claim.Category = updatedClaim.Category;

            // Update ProviderId and InsurerId if provided (validate they exist)
            if (updatedClaim.ProviderId > 0 && updatedClaim.ProviderId != claim.ProviderId)
            {
                var provider = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == updatedClaim.ProviderId && u.Role == UserRole.Provider);
                if (provider != null)
                {
                    claim.ProviderId = updatedClaim.ProviderId;
                }
            }

            if (updatedClaim.InsurerId > 0 && updatedClaim.InsurerId != claim.InsurerId)
            {
                var insurer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == updatedClaim.InsurerId && u.Role == UserRole.Insurer);
                if (insurer != null)
                {
                    claim.InsurerId = updatedClaim.InsurerId;
                }
            }

            claim.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(new AuditLog
            {
                Action = AuditAction.Update,
                EntityType = EntityType.Claim,
                EntityId = id.ToString(),
                ActionDescription = "Claim updated"
            });

            return claim;
        }
    }
}


