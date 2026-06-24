using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Mathy.ELM.Infrastructure.Data;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.Authorization;
using Mathy.ELM.Core.Configuration;
using Mathy.ELM.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Mathy.ELM.Api;
using Mathy.ELM.Api.Hubs;
using Mathy.ELM.Api.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with separate log files
var logPath = builder.Configuration.GetValue<string>("Logging:FilePath") ?? "Logs";
var retainedFileCountLimit = 8; // Keep logs for 8 days (FIFO)

// Create separate loggers for different purposes
var appLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_app_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] [{Status}] {Message:lj}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var errorLogger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_error_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var migrationLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_efMigrationsHistory_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Operation}] [{Status}] {Message:lj}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var emailLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_email_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] [{Status}] {Message:lj}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var jobsLogger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_jobs_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] [{Status}] {Message:lj}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var adErrorLogger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_error_AD_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

var emailNotifErrorLogger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: Path.Combine(logPath, "ecm_log_error_EmailNotif_.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retainedFileCountLimit,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties:j}{NewLine}{NewLine}")
    .CreateLogger();

// Configure host to use Serilog for console output only (file logging is handled by EcmLogger)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Register EcmLogger as singleton with all seven separate loggers
builder.Services.AddSingleton<IEcmLogger>(sp => new EcmLogger(appLogger, errorLogger, migrationLogger, emailLogger, jobsLogger, adErrorLogger, emailNotifErrorLogger));

// Add services to the container.
builder.Services.AddDbContext<MathyELMContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Viewpoint API settings
builder.Services.Configure<ViewpointApiSettings>(
    builder.Configuration.GetSection(ViewpointApiSettings.SectionName));

// Register HttpClient for Viewpoint service
builder.Services.AddHttpClient<IViewpointService, ViewpointService>();

builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IViewpointService, ViewpointService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IRoleFilterService, RoleFilterService>();
builder.Services.AddScoped<IHRRequestService, HRRequestService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IReturnToWorkRequestDetailsService, ReturnToWorkRequestDetailsService>();
builder.Services.AddScoped<ILayoffRequestDetailsService, LayoffRequestDetailsService>();
builder.Services.AddScoped<ITerminationRequestDetailsService, TerminationRequestDetailsService>();
builder.Services.AddScoped<INewHireRequestDetailsService, NewHireRequestDetailsService>();
builder.Services.AddScoped<IPromotionRequestDetailsService, PromotionRequestDetailsService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register ServiceDesk Integration Service
builder.Services.AddHttpClient<IServiceDeskIntegrationService, ServiceDeskIntegrationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(300); // 5 minutes for ServiceDesk API
});

// Register Email Service - use mock in development to avoid Azure Communication Service dependency
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local"))
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, AzureEmailService>();
}
// Register Azure Service Bus Email Service (independent from IEmailService)
// Changed from Singleton to Scoped to allow access to scoped dependencies (DbContext, etc.)
builder.Services.AddScoped<IAzureServiceBusEmailService, AzureServiceBusEmailService>();

// Register Email Template Services
builder.Services.AddScoped<IEmailFieldMapperService, EmailFieldMapperService>();
builder.Services.AddScoped<IEmailTemplateBuilderService, EmailTemplateBuilderService>();
builder.Services.AddScoped<IEmailRecipientsService, EmailRecipientsService>();

builder.Services.AddHttpContextAccessor();

// Configure SignalR
builder.Services.AddSignalR(options =>
{
    // Enable detailed errors for development
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    // Configure server to process jobs from ALL queues
    // By NOT specifying Queues, Hangfire will listen to all queues
    // This ensures scheduled emails in newhire-email-* queues are processed
    options.WorkerCount = 20; // Match the number of workers shown in dashboard
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.Audience = builder.Configuration["AzureAd:Audience"] ?? builder.Configuration["AzureAd:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false, // Temporarily disable to test other validations
            RequireSignedTokens = false, // Also disable this for testing
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
            
            // Set valid issuers for Azure AD (multiple formats to handle different token types)
            ValidIssuers = new[]
            {
                $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0", // v2.0 endpoint
                $"https://sts.windows.net/{builder.Configuration["AzureAd:TenantId"]}/", // v1.0 endpoint
                $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/", // Alternative format
                "https://login.microsoftonline.com/common/v2.0", // Multi-tenant v2.0
                "https://sts.windows.net/common/", // Multi-tenant v1.0
                $"https://login.microsoftonline.com/aa5e36ca-b6c1-4565-8261-0b02ac026bce/v2.0", // Explicit tenant
                $"https://sts.windows.net/aa5e36ca-b6c1-4565-8261-0b02ac026bce/" // Explicit tenant v1.0
            },
            
            // Accept multiple valid audiences from configuration
            ValidAudiences = new[]
            {
                builder.Configuration["AzureAd:ClientId"], // Current ClientId
                builder.Configuration["AzureAd:Audience"], // Custom audience from config
                $"api://{builder.Configuration["AzureAd:ClientId"]}", // API scope format
                "00000003-0000-0000-c000-000000000000" // Microsoft Graph
            }.Where(a => !string.IsNullOrEmpty(a)).ToArray(),
            
            // Additional validation parameters for better compatibility
            ValidateTokenReplay = false,
            RequireExpirationTime = true,
            SaveSigninToken = false
        };
        
        // Force metadata refresh to get latest signing keys
        options.MetadataAddress = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0/.well-known/openid-configuration";
        options.RequireHttpsMetadata = true;
        options.RefreshOnIssuerKeyNotFound = true;
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5); // More frequent refresh for development
        options.BackchannelTimeout = TimeSpan.FromSeconds(60);
    });

builder.Services.AddAuthorization(options =>
{
    // Add role-based policies
    options.AddPolicy("HRAdmin", policy => policy.RequireRole("HRAdmin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager", "HRAdmin"));
    options.AddPolicy("SystemAdmin", policy => policy.RequireRole("SystemAdmin"));
    
    // Company-based policies will be added dynamically
    // e.g., "CompanyAccess:001" for company 001
});

builder.Services.AddScoped<IAuthorizationHandler, CompanyAuthorizationHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Mathy ELM API",
        Version = "v1",
        Description = "Employee Change Management System API - Dynamically configured based on launch settings",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Mathy Development Team",
            Email = "dev@mathy.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Internal Use"
        }
    });

    // Configure JWT Bearer authentication for Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS with allowed origins from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        }
        else
        {
            // Fallback to localhost for development if no origins configured
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mathy ELM API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        
        // Configure OAuth2 for Azure AD (optional)
        c.OAuthClientId("your-client-id");
        c.OAuthAppName("Mathy ELM API");
        c.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Map SignalR Hub
app.MapHub<HRRequestStatusHub>("/hubs/hr-request-status")
   .RequireCors("AllowAngularApp");

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var ecmLogger = scope.ServiceProvider.GetRequiredService<IEcmLogger>();

    try
    {
        logger.LogInformation("Checking for pending database migrations...");
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
        var migrationStartTime = DateTime.UtcNow;

        // Log migration check using EcmLogger
        ecmLogger.LogMigrationCheck(pendingMigrations.Count, pendingMigrations);

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));

            // Log each migration start
            foreach (var migration in pendingMigrations)
            {
                ecmLogger.LogMigrationStart(migration);
            }

            var applyStartTime = DateTime.UtcNow;
            await context.Database.MigrateAsync();
            var applyDuration = DateTime.UtcNow - applyStartTime;

            // Log migration completion
            foreach (var migration in pendingMigrations)
            {
                ecmLogger.LogMigration(true, migration, "APPLIED");
            }

            ecmLogger.LogMigrationSummary(pendingMigrations.Count, 0, applyDuration);
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending migrations found. Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        ecmLogger.LogMigrationFailed("DatabaseMigration", ex);
        throw; // Re-throw to prevent application startup with database issues
    }

    // Ensure Azure Service Bus queue exists
    try
    {
        var azureServiceBusEmailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var queueCreated = await azureServiceBusEmailService.EnsureQueueExistsAsync();
        if (queueCreated)
        {
            logger.LogInformation("Azure Service Bus email queue is ready");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to ensure Azure Service Bus queue exists. Service may not be configured.");
    }

    // Setup recurring background jobs
    try
    {
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        backgroundJobService.SetupNewHireEmailNotificationsJob();
        logger.LogInformation("New Hire email notification job registered successfully");

        // Setup draft reminder email job
        backgroundJobService.SetupDraftReminderEmailJob();
        logger.LogInformation("Draft reminder email job registered successfully");

        // Setup Welcome Email notification job
        backgroundJobService.SetupWelcomeEmailScheduledJob();
        logger.LogInformation("Welcome Email notification job registered successfully");

        // Setup Return to Work email notification job
        backgroundJobService.SetupReturnToWorkEmailNotificationsJob();
        logger.LogInformation("Return to Work email notification job registered successfully");

        // Setup Layoff email notification job
        backgroundJobService.SetupLayoffEmailNotificationsJob();
        logger.LogInformation("Layoff email notification job registered successfully");

        // Setup Termination email notification job
        backgroundJobService.SetupTerminationEmailNotificationsJob();
        logger.LogInformation("Termination email notification job registered successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to register background jobs");
    }
}

app.Run();
