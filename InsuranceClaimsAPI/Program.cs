using InsuranceClaimsAPI.Configuration;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Hubs;
using InsuranceClaimsAPI.Services;
using InsuranceClaimsAPI.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;

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

// Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings
{
    Secret = builder.Configuration.GetValue<string>("JwtSettings:Secret") ?? 
             "Your-Very-Long-Super-Secret-Key-Default-ChangeInProduction",
    Issuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer") ?? "InsuranceClaimsAPI",
    Audience = builder.Configuration.GetValue<string>("JwtSettings:Audience") ?? "InsuranceClaimsClient",
    TokenExpiryInMinutes = builder.Configuration.GetValue<int>("JwtSettings:TokenExpiryInMinutes", 60),
    RefreshTokenExpiryInDays = builder.Configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryInDays", 7)
};

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Log.Warning("JWT challenge occurred");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireProviderRole", policy =>
        policy.RequireRole("Provider"));
    options.AddPolicy("RequireInsurerRole", policy =>
        policy.RequireRole("Insurer"));
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
});

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

// Initialize Firebase
FirebaseConfig.InitializeFirebase(builder.Configuration, Log.Logger);

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

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowReactApp");

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// API endpoints
app.MapControllers();

// SignalR hubs
app.MapHub<MessageHub>("/messageHub");
app.MapHub<NotificationHub>("/notificationHub");

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

app.Run();

// Configure Logging
Log.Information("Insurance Claims API is shutting down");

Log.CloseAndFlush();