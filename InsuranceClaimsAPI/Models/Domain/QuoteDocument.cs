using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum QuoteDocumentType
    {
        DetailedEstimate = 1,
        MaterialSpecification = 2,
        LaborBreakdown = 3,
        EquipmentRental = 4,
        PermitFees = 5,
        TimelineDocument = 6,
        WarrantyDocument = 7,
        TechnicalDrawings = 8,
        Other = 9
    }

    [Table("QuoteDocuments")]
    public class QuoteDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuoteId { get; set; }

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

        public QuoteDocumentType Type { get; set; }

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Tags { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("QuoteId")]
        public virtual Quote Quote { get; set; } = null!;

        [ForeignKey("UploadedById")]
        public virtual User UploadedBy { get; set; } = null!;
    }
}
