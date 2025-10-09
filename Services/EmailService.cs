namespace InsuranceClaimsAPI.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            // Placeholder implementation. Integrate with a real email provider.
            _logger.LogInformation("Email to {Email} | {Subject}", toEmail, subject);
            return Task.CompletedTask;
        }
    }
}


