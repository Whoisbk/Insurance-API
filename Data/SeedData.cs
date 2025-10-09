using InsuranceClaimsAPI.Models.Domain;
using Serilog;

namespace InsuranceClaimsAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(InsuranceClaimsContext context, Serilog.ILogger logger)
        {
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User
                    {
                        FirstName = "Admin",
                        LastName = "User",
                        Email = "admin@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
                        Role = UserRole.Admin,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Insurance",
                        LastName = "Company",
                        Email = "insurer@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Insurer@12345"),
                        Role = UserRole.Insurer,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Service",
                        LastName = "Provider",
                        Email = "provider@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider@12345"),
                        Role = UserRole.Provider,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                );

                await context.SaveChangesAsync();
                logger.Information("Seeded default users");
            }
        }
    }
}


