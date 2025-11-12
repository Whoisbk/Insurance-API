using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace InsuranceClaimsAPI.Configuration
{
    public static class FirebaseConfig
    {
        public static void InitializeFirebase(IConfiguration configuration, Serilog.ILogger logger)
        {
            try
            {
                // Check if Firebase is already initialized
                if (FirebaseApp.DefaultInstance != null)
                {
                    logger.Information("Firebase is already initialized");
                    return;
                }

                // Get Firebase configuration from appsettings or environment variables
                var projectId = configuration["Firebase:ProjectId"] ?? Environment.GetEnvironmentVariable("Firebase__ProjectId");
                var privateKey = configuration["Firebase:PrivateKey"] ?? configuration["Firebase:private_key"] ?? Environment.GetEnvironmentVariable("Firebase__PrivateKey");
                var clientEmail = configuration["Firebase:ClientEmail"] ?? Environment.GetEnvironmentVariable("Firebase__ClientEmail");

                if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(clientEmail))
                {
                    logger.Warning("Firebase configuration is incomplete. Firebase Admin SDK will not be initialized.");
                    return;
                }

                // Handle private key format - replace literal \n with actual newlines
                // This is important when setting keys via environment variables
                privateKey = privateKey.Replace("\\n", "\n");

                // Validate private key format
                if (!privateKey.Contains("BEGIN PRIVATE KEY") || !privateKey.Contains("END PRIVATE KEY"))
                {
                    logger.Error("Firebase private key format is invalid. It should include BEGIN/END PRIVATE KEY markers.");
                    throw new InvalidOperationException("Invalid Firebase private key format");
                }

                // Create credentials from configuration
                var credentials = GoogleCredential.FromServiceAccountCredential(
                    new ServiceAccountCredential(new ServiceAccountCredential.Initializer(clientEmail)
                    {
                        ProjectId = projectId
                    }.FromPrivateKey(privateKey)));

                // Initialize Firebase Admin SDK
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credentials,
                    ProjectId = projectId
                });

                logger.Information("Firebase Admin SDK initialized successfully for project: {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize Firebase Admin SDK");
                throw;
            }
        }

        public static void AddFirebaseServices(this IServiceCollection services)
        {
            // Register Firebase services if needed
            // For now, we'll use FirebaseAuth.DefaultInstance directly in controllers
        }
    }
}
