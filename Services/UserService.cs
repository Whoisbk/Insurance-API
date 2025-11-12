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

            var insurerRecord = new Insurer
            {
                UserId = insurer.Id,
                Name = $"{insurer.FirstName} {insurer.LastName}".Trim(),
                Email = insurer.Email,
                PhoneNumber = insurer.PhoneNumber,
                Address = insurer.Address,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Insurers.Add(insurerRecord);
            await _context.SaveChangesAsync();
            insurer.InsurerProfile = insurerRecord;
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
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByFirebaseUidAsync(string firebaseUid)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null)
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
            var user = await _context.Users
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return false;

            // Soft delete: Set DeletedAt timestamp instead of actually deleting
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null)
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> EmailExistsForAnotherUserAsync(int userId, string email)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null)
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == email.ToLower());
        }

        public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
        {
            IQueryable<User> query = _context.Users
                .Where(u => u.Role == role && u.DeletedAt == null);

            if (role == UserRole.Provider)
            {
                query = query
                    .Include(u => u.Quotes)
                    .Include(u => u.Notifications);
            }

            return await query
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> GetProviderByIdWithDetailsAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id && u.Role == UserRole.Provider && u.DeletedAt == null)
                .Include(u => u.Quotes)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetInsurerByIdWithDetailsAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id && u.Role == UserRole.Insurer && u.DeletedAt == null)
                .Include(u => u.ManagedClaims)
                    .ThenInclude(c => c.Provider)
                .Include(u => u.ManagedClaims)
                    .ThenInclude(c => c.Quotes)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync();
        }
    }
}
