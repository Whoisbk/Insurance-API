using System.ComponentModel.DataAnnotations;
using InsuranceClaimsAPI.Models.Domain;

namespace InsuranceClaimsAPI.Models.DTOs.Claims
{
    public class CreateClaimForProviderRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "InsurerId must be a valid positive number")]
        public int InsurerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProviderId must be a valid positive number")]
        public int ProviderId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public ClaimPriority Priority { get; set; } = ClaimPriority.Medium;

        [Required]
        [Range(0.01, 999999999.99, ErrorMessage = "EstimatedAmount must be greater than 0")]
        public decimal EstimatedAmount { get; set; }

        [MaxLength(100)]
        public string? PolicyNumber { get; set; }

        [MaxLength(100)]
        public string? PolicyHolderName { get; set; }

        [MaxLength(255)]
        public string? IncidentLocation { get; set; }

        public DateTime? IncidentDate { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }
    }
}

