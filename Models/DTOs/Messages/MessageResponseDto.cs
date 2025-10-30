using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Models.DTOs.Messages
{
    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public int? QuoteId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderEmail { get; set; }
        public int? ReceiverId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverEmail { get; set; }
        public MessageType Type { get; set; }
        public MessageStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentMimeType { get; set; }
        public long? AttachmentSizeBytes { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsImportant { get; set; }
        public int? ReplyToMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

