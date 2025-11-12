using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;

namespace InsuranceClaimsAPI.Services
{
    public interface IServiceProviderService
    {
        Task<Models.Domain.ServiceProvider> CreateServiceProviderAsync(int userId, CreateProviderRequest request);
        Task<Models.Domain.ServiceProvider?> GetServiceProviderByUserIdAsync(int userId);
        Task<Models.Domain.ServiceProvider?> GetServiceProviderByIdAsync(int id);
        Task<Models.Domain.ServiceProvider> UpdateServiceProviderAsync(Models.Domain.ServiceProvider serviceProvider);
        Task<bool> DeleteServiceProviderAsync(int id);
        Task<List<Models.Domain.ServiceProvider>> GetAllServiceProvidersAsync();
        Task<List<Models.Domain.ServiceProvider>> GetServiceProvidersByInsurerAsync(int insurerId);
    }
}
