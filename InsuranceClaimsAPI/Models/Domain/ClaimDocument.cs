using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum DocumentType
    {
        IncidentReport = 1,
        Photos = 2,
        Estimates = 3,
        Receipts = 4,
        PolicyDocuments = 5,
        ContractorLicense = 6,
        InsuranceDocuments = 7,
        Other = 8
    }

    [Table("ClaimDocuments")]
    public class ClaimDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        public int UploadedById { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(50)]
        public string FileExtension { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public DocumentType Type { get; set; }

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? Tags { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ClaimId")]
        public virtual Claim Claim { get; set; } = null!;

        [ForeignKey("UploadedById")]
        public virtual User UploadedBy { get; set; } = null!;
    }
}
