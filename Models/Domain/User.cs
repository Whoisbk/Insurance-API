using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum UserRole
    {
        Insurer = 1,
        Provider = 2,
        Admin = 3
    }

    public enum UserStatus
    {
        Active = 1,
        Inactive = 2,
        Suspended = 3
    }

    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FirebaseUid { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }

        [MaxLength(5000)]
        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        [InverseProperty("Provider")]
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
        
        [InverseProperty("Provider")]
        public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
        
        [InverseProperty("sender")]
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        
        public virtual ICollection<ClaimDocument> ClaimDocuments { get; set; } = new List<ClaimDocument>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        [InverseProperty("Insurer")]
        public virtual ICollection<Claim> ManagedClaims { get; set; } = new List<Claim>();
    }
}
