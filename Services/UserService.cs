using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Admin;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public class UserService : IUserService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IServiceProviderService _serviceProviderService;

        public UserService(InsuranceClaimsContext context, IServiceProviderService serviceProviderService)
        {
            _context = context;
            _serviceProviderService = serviceProviderService;
        }

        public async Task<User> CreateInsurerAsync(User insurer)
        {
            insurer.Role = UserRole.Insurer;
            insurer.Status = UserStatus.Active;
            insurer.CreatedAt = DateTime.UtcNow;
            insurer.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(insurer);
            await _context.SaveChangesAsync();
            return insurer;
        }

        public async Task<User> CreateProviderAsync(User provider)
        {
            provider.Role = UserRole.Provider;
            provider.Status = UserStatus.Active;
            provider.CreatedAt = DateTime.UtcNow;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(provider);
            await _context.SaveChangesAsync();
            return provider;
        }

        public async Task<User> CreateProviderWithServiceProviderAsync(User provider, CreateProviderRequest request)
        {
            // Create the user first
            provider.Role = UserRole.Provider;
            provider.Status = UserStatus.Active;
            provider.CreatedAt = DateTime.UtcNow;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(provider);
            await _context.SaveChangesAsync();

            // Create the ServiceProvider record
            await _serviceProviderService.CreateServiceProviderAsync(provider.Id, request);

            return provider;
        }

        public async Task<User> CreateAdminAsync(User admin)
        {
            admin.Role = UserRole.Admin;
            admin.Status = UserStatus.Active;
            admin.CreatedAt = DateTime.UtcNow;
            admin.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            return admin;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByFirebaseUidAsync(string firebaseUid)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }
    }
}
