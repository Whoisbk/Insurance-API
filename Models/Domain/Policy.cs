using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceClaimsAPI.Models.Domain
{
    public enum PolicyType
    {
        Vehicle = 1,
        Home = 2,
        Medical = 3
    }

    [Table("Policies")]
    public class Policy
    {
        [Key]
        public int PolicyId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public PolicyType PolicyType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Navigation properties
        [ForeignKey("ClientId")]
        public virtual User Client { get; set; } = null!;
    }
}
