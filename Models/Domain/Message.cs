using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum MessageType
    {
        Text = 1,
        Attachment = 2,
        QuoteSubmission = 3,
        QuoteApproval = 4,
        QuoteRejection = 5,
        QuoteRevision = 6,
        StatusUpdate = 7,
        SystemNotification = 8
    }

    public enum MessageStatus
    {
        Sent = 1,
        Delivered = 2,
        Read = 3,
        Failed = 4
    }

    [Table("Messages")]
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClaimId { get; set; }

        public int? QuoteId { get; set; }

        [Required]
        public int SenderId { get; set; }

        public int? ReceiverId { get; set; }

        public MessageType Type { get; set; }
        public MessageStatus Status { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Subject { get; set; }

        [MaxLength(255)]
        public string? AttachmentUrl { get; set; }

        [MaxLength(255)]
        public string? AttachmentFileName { get; set; }

        [MaxLength(50)]
        public string? AttachmentMimeType { get; set; }

        public long? AttachmentSizeBytes { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public bool IsImportant { get; set; }
        public bool IsInternal { get; set; }

        [MaxLength(255)]
        public string? ReplyToMessageId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ClaimId")]
        public virtual Claim Claim { get; set; } = null!;

        [ForeignKey("QuoteId")]
        public virtual Quote? Quote { get; set; }

        [ForeignKey("SenderId")]
        public virtual User sender { get; set; } = null!;

        [ForeignKey("ReceiverId")]
        public virtual User? Receiver { get; set; }

        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
