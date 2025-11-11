using InsuranceClaimsAPI.Configuration;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Hubs;
using InsuranceClaimsAPI.Services;
using InsuranceClaimsAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/insurance-claims-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Serialize enums as integers (default) so role/status return numeric values
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<InsuranceClaimsContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        ConnectionStrings.DefaultConnection,
        new MySqlServerVersion(new Version(8, 0, 21))
    )
    .LogTo(Log.Information, LogLevel.Information)
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
);


// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001") // React development server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Initialize Firebase (non-blocking - app will continue even if Firebase fails)
try
{
    FirebaseConfig.InitializeFirebase(builder.Configuration, Log.Logger);
}
catch (Exception ex)
{
    Log.Error(ex, "Firebase initialization failed, but application will continue. Some features may not work.");
    // Don't throw - allow app to start without Firebase if needed
}

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IServiceProviderService, ServiceProviderService>();



// Repository pattern services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// Resend Email SDK configuration (supports ENV and appsettings fallback)
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    var envToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN");
    var cfgToken = builder.Configuration["Email:ResendApiKey"]; 
    o.ApiToken = !string.IsNullOrWhiteSpace(envToken) ? envToken! : (cfgToken ?? string.Empty);
});
builder.Services.AddTransient<IResend, ResendClient>();

// Memory cache for SignalR
builder.Services.AddMemoryCache();

// SignalR
builder.Services.AddSignalR();

// HTTP Context Accessor for getting current user
builder.Services.AddHttpContextAccessor();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<InsuranceClaimsContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Only redirect HTTPS in development - Render handles HTTPS termination
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowReactApp");

// Health check endpoint
app.MapHealthChecks("/health");

// API endpoints
app.MapControllers();

// SignalR hubs
app.MapHub<MessageHub>("/messageHub");
app.MapHub<NotificationHub>("/notificationHub");
// Aliases to match frontend expectations
app.MapHub<MessageHub>("/hubs/messages");
app.MapHub<NotificationHub>("/hubs/notifications");

// Create database if it doesn't exist and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InsuranceClaimsContext>();
    try
    {
        context.Database.EnsureCreated();
        
        // Seed initial data if needed
        await SeedData.Initialize(context, Log.Logger);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

// Configure port for Render deployment (uses PORT env var if available)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();

// Configure Logging
Log.Information("Insurance Claims API is shutting down");

Log.CloseAndFlush();