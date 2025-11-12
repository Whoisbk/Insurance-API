namespace InsuranceClaimsAPI.Models.DTOs
{
    public class RecentActivityDto
    {
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
