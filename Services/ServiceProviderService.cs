using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public class ServiceProviderService : IServiceProviderService
    {
        private readonly InsuranceClaimsContext _context;

        public ServiceProviderService(InsuranceClaimsContext context)
        {
            _context = context;
        }

        public async Task<Models.Domain.ServiceProvider> CreateServiceProviderAsync(int userId, CreateProviderRequest request)
        {
            var serviceProvider = new Models.Domain.ServiceProvider
            {
                UserId = userId,
                InsurerId = request.InsurerId,
                Name = $"{request.FirstName} {request.LastName}",
                Specialization = "General Services", // Default specialization, can be updated later
                PhoneNumber = request.PhoneNumber ?? "",
                Email = request.Email,
                Address = request.Address ?? "",
                EndDate = DateTime.UtcNow.AddYears(1) // Default 1 year policy
            };

            _context.ServiceProviders.Add(serviceProvider);
            await _context.SaveChangesAsync();
            return serviceProvider;
        }

        public async Task<Models.Domain.ServiceProvider?> GetServiceProviderByUserIdAsync(int userId)
        {
            return await _context.ServiceProviders
                .Include(sp => sp.User)
                .Include(sp => sp.Insurer)
                    .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(sp => sp.UserId == userId);
        }

        public async Task<Models.Domain.ServiceProvider?> GetServiceProviderByIdAsync(int id)
        {
            return await _context.ServiceProviders
                .Include(sp => sp.User)
                .Include(sp => sp.Insurer)
                .FirstOrDefaultAsync(sp => sp.ProviderId == id);
        }

        public async Task<Models.Domain.ServiceProvider> UpdateServiceProviderAsync(Models.Domain.ServiceProvider serviceProvider)
        {
            _context.ServiceProviders.Update(serviceProvider);
            await _context.SaveChangesAsync();
            return serviceProvider;
        }

        public async Task<bool> DeleteServiceProviderAsync(int id)
        {
            var serviceProvider = await _context.ServiceProviders.FindAsync(id);
            if (serviceProvider == null)
                return false;

            _context.ServiceProviders.Remove(serviceProvider);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Models.Domain.ServiceProvider>> GetAllServiceProvidersAsync()
        {
            return await _context.ServiceProviders
                .Include(sp => sp.User)
                .Include(sp => sp.Insurer)
                .OrderBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<List<Models.Domain.ServiceProvider>> GetServiceProvidersByInsurerAsync(int insurerId)
        {
            return await _context.ServiceProviders
                .Include(sp => sp.User)
                .Include(sp => sp.Insurer)
                .Where(sp => sp.InsurerId == insurerId)
                .OrderBy(sp => sp.Name)
                .ToListAsync();
        }

    }
}
