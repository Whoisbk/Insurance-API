using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Models.DTOs.Auth
{
    public class AuthResponseDto
    {
        public UserDto User { get; set; } = null!;
        public string Message { get; set; } = "Authentication successful";
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirebaseUid { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? InsurerId { get; set; }
        public int? ServiceProviderId { get; set; }
    }
}
