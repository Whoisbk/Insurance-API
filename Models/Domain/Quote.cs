using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum QuoteStatus
    {
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Revised = 4
    }

    [Table("Quotes")]
    public class Quote
    {
        [Key]
        public int QuoteId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public QuoteStatus Status { get; set; }

        public DateTime DateSubmitted { get; set; }

        // Navigation properties
        [ForeignKey("PolicyId")]
        public virtual Claim Policy { get; set; } = null!;

        [ForeignKey("ProviderId")]
        public virtual User Provider { get; set; } = null!;

        public virtual ICollection<QuoteDocument> QuoteDocuments { get; set; } = new List<QuoteDocument>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
