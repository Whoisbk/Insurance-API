using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum ClaimStatus
    {
        Draft = 1,
        Submitted = 2,
        UnderReview = 3,
        Approved = 4,
        Rejected = 5,
        Closed = 6
    }

    public enum ClaimPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    [Table("Claims")]
    public class Claim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ClaimNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public int InsurerId { get; set; }

        public ClaimStatus Status { get; set; }
        public ClaimPriority Priority { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedAmount { get; set; }

        [MaxLength(100)]
        public string? PolicyNumber { get; set; }

        [MaxLength(100)]
        public string? PolicyHolderName { get; set; }

        [MaxLength(150)]
        public string? ClientFullName { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        public string? ClientEmailAddress { get; set; }

        [MaxLength(50)]
        public string? ClientPhoneNumber { get; set; }

        [MaxLength(500)]
        public string? ClientAddress { get; set; }

        [MaxLength(255)]
        public string? ClientCompany { get; set; }

        [MaxLength(255)]
        public string? IncidentLocation { get; set; }

        public DateTime? IncidentDate { get; set; }
        public DateTime? ClaimSubmittedDate { get; set; }
        public DateTime? DueDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ProviderId")]
        public virtual User? Provider { get; set; }

        [ForeignKey("InsurerId")]
        public virtual User? Insurer { get; set; }

        public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<ClaimDocument> ClaimDocuments { get; set; } = new List<ClaimDocument>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
