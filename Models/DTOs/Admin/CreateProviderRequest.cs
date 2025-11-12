using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimsAPI.Models.DTOs.Admin
{
    public class CreateProviderRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MinLength(6)]
        public string? Password { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "InsurerId must be a positive integer.")]
        public int InsurerId { get; set; }

        /// <summary>
        /// Optional role field (will be ignored, always set to Provider)
        /// </summary>
        public int? Role { get; set; }

        /// <summary>
        /// Terms acceptance flag
        /// </summary>
        public bool? AcceptTerms { get; set; }
    }
}
