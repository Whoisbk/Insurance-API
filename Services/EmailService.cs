using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Resend;

namespace InsuranceClaimsAPI.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _fromAddress;
        private readonly IResend? _resend;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration, IResend? resend = null)
        {
            _logger = logger;
            
            // Prefer env var then appsettings (kept for HTTP fallback)
            var envToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN");
            _apiKey = envToken ?? configuration["Email:ResendApiKey"] ?? string.Empty;

            // Use configured From or default to a simple valid address
            _fromAddress = configuration["Email:From"] ?? "noreply@metimeonline.co.za";
            
            _resend = resend;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.resend.com")
            };
            
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }
            
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            // Prefer official SDK when available
            if (_resend != null)
            {
                try
                {
                    var message = new EmailMessage
                    {
                        From = _fromAddress,
                        To = toEmail,
                        Subject = subject,
                        HtmlBody = htmlBody
                    };
                    
                    if (!string.IsNullOrWhiteSpace(textBody))
                    {
                        message.TextBody = textBody;
                    }

                    await _resend.EmailSendAsync(message);
                    _logger.LogInformation("Email sent successfully to {Email}. Subject: {Subject}", toEmail, subject);

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Resend SDK send failed, will attempt HTTP fallback");
                }
            }

            // HTTP Fallback
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Resend API key not configured. Skipping email to {Email}. Subject: {Subject}", 
                    toEmail, subject);
                return;
            }

            var payload = new
            {
                from = _fromAddress,
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody,
                text = textBody
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/emails", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Resend API error: {Status} - {Content}", 
                        response.StatusCode, responseContent);
                    return;
                }

                _logger.LogInformation("Email sent to {Email}. Subject: {Subject}. Response: {Response}", 
                    toEmail, subject, responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", 
                    toEmail, subject);
            }
        }
    }
}