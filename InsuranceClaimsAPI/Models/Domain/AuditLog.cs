using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum AuditAction
    {
        Create = 1,
        Read = 2,
        Update = 3,
        Delete = 4,
        Login = 5,
        Logout = 6,
        QuoteStatusChange = 7,
        ClaimStatusChange = 8,
        MessageSent = 9,
        DocumentUploaded = 10,
        PermissionChanged = 11,
        PasswordChanged = 12,
        ProfileUpdated = 13
    }

    public enum EntityType
    {
        User = 1,
        Claim = 2,
        Quote = 3,
        Message = 4,
        Document = 5,
        Login = 6,
        System = 7
    }

    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public AuditAction Action { get; set; }
        public EntityType EntityType { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(255)]
        public string ActionDescription { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? OldValues { get; set; }

        [MaxLength(255)]
        public string? NewValues { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? RequestId { get; set; }

        [MaxLength(255)]
        public string? SessionId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
