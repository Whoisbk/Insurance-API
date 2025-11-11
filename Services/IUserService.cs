using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;

namespace InsuranceClaimsAPI.Services
{
    public interface IUserService
    {
        Task<User> CreateInsurerAsync(User insurer);
        Task<User> CreateProviderAsync(User provider);
        Task<User> CreateProviderWithServiceProviderAsync(User provider, CreateProviderRequest request);
        Task<User> CreateAdminAsync(User admin);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByFirebaseUidAsync(string firebaseUid);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmailExistsForAnotherUserAsync(int userId, string email);
        Task<List<User>> GetUsersByRoleAsync(UserRole role);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetProviderByIdWithDetailsAsync(int id);
        Task<User?> GetInsurerByIdWithDetailsAsync(int id);
    }
}
