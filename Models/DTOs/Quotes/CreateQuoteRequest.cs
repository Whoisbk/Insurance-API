using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimsAPI.Models.DTOs.Quotes
{
    public class CreateQuoteRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ClaimId must be a valid positive number")]
        public int ClaimId { get; set; }

        [Required]
        [Range(0.01, 999999999.99, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}

