using System.ComponentModel.DataAnnotations;
using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Models.DTOs.Messages
{
    public class CreateQuoteMessageRequest
    {
        [Required]
        public int QuoteId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Subject { get; set; }

        public MessageType Type { get; set; } = MessageType.Text;

        [MaxLength(255)]
        public string? AttachmentUrl { get; set; }

        [MaxLength(255)]
        public string? AttachmentFileName { get; set; }

        [MaxLength(50)]
        public string? AttachmentMimeType { get; set; }

        public long? AttachmentSizeBytes { get; set; }

        public int? ReplyToMessageId { get; set; }
    }
}

