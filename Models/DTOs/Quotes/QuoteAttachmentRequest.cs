using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Models.DTOs.Quotes
{
    public class QuoteAttachmentRequest : IValidatableObject
    {
        [MaxLength(255)]
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FileNameAlias
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    FileName = value;
                }
            }
        }

        /// <summary>
        /// Base64 encoded file content. May include data URI prefix.
        /// Optional when a remote URL is provided.
        /// </summary>
        [JsonPropertyName("contentBase64")]
        public string? ContentBase64 { get; set; }
            = null;

        [JsonPropertyName("url")]
        public string? Url { get; set; }
            = null;

        [JsonPropertyName("storagePath")]
        public string? StoragePath { get; set; }
            = null;

        [JsonPropertyName("uploadedAt")]
        public DateTime? UploadedAt { get; set; }
            = null;

        [JsonPropertyName("size")]
        public long? FileSizeBytes { get; set; }
            = null;

        [MaxLength(100)]
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }
            = "application/octet-stream";

        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MimeTypeAlias
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    MimeType = value;
                }
            }
        }

        [JsonPropertyName("documentType")]
        public QuoteDocumentType? DocumentType { get; set; }
            = QuoteDocumentType.Other;

        [MaxLength(255)]
        public string? Title { get; set; }
            = null;

        [MaxLength(500)]
        public string? Description { get; set; }
            = null;

        [MaxLength(255)]
        public string? Tags { get; set; }
            = null;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                yield return new ValidationResult(
                    "FileName is required",
                    new[] { nameof(FileName) });
            }

            if (string.IsNullOrWhiteSpace(ContentBase64) && string.IsNullOrWhiteSpace(Url) && string.IsNullOrWhiteSpace(StoragePath))
            {
                yield return new ValidationResult(
                    "Either ContentBase64 or a Url/StoragePath must be provided",
                    new[] { nameof(ContentBase64), nameof(Url), nameof(StoragePath) });
            }
        }
    }
}


