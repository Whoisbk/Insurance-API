using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IClaimService
    {
        Task<Claim> CreateAsync(Claim claim);
        Task<Claim?> GetAsync(int id);
        Task<IReadOnlyList<Claim>> GetForUserAsync(int userId);
        Task UpdateStatusAsync(int claimId, ClaimStatus status);
    }

    public class ClaimService : IClaimService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IAuditService _auditService;

        public ClaimService(InsuranceClaimsContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
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


