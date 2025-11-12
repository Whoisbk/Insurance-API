using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimsAPI.Models.DTOs.Admin
{
    public class UpdateServiceProviderRequest
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public DateTime? EndDate { get; set; }

        public int? InsurerId { get; set; }

        // User fields
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? UserEmail { get; set; }
    }
}

