using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Services
{
    public interface IAuditService
    {
        Task LogAsync(AuditLog log);
    }

    public class AuditService : IAuditService
    {
        private readonly InsuranceClaimsContext _context;

        public AuditService(InsuranceClaimsContext context)
        {
            _context = context;
        }

        public async Task LogAsync(AuditLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}


