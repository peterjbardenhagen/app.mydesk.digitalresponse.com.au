using System.Threading.RateLimiting;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using MyDesk.Shared.Data;
using MyDesk.Shared.Services;
using MyDesk.Shared.Models;
using MyDesk.Web.Components;
using MyDesk.Web.Services;
using MyDesk.Web.Scheduling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------------
// Logging (Serilog) - writes to /Logs/app-YYYYMMDD.log and /Logs/errors-YYYYMMDD.log
// ---------------------------------------------------------------------------
// Logs go to the PROJECT ROOT /Logs folder (not bin/Debug/...), so they
// survive rebuilds and are easy to find.
var projectRoot = Directory.GetCurrentDirectory(); // cwd when "dotnet run" is invoked
var logsDir = Path.Combine(projectRoot, "Logs");
Directory.CreateDirectory(logsDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(logsDir, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ThreadId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine(logsDir, "errors-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 60,
        restrictedToMinimumLevel: LogEventLevel.Warning,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ThreadId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("======================================================================");
Log.Information("MyDesk Platform starting up - PID {Pid}, Logs at {LogsDir}", Environment.ProcessId, logsDir);
Log.Information("Powered by Digital Response - https://www.digitalresponse.com.au");
Log.Information("======================================================================");

// Catch anything the framework misses (e.g. background threads / finalizers)
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    Log.Fatal(e.ExceptionObject as Exception, "UNHANDLED AppDomain exception (IsTerminating={Terminating})", e.IsTerminating);
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Log.Error(e.Exception, "UNHANDLED Task exception");
    e.SetObserved();
};

try
{

var builder = WebApplication.CreateBuilder(args);
Microsoft.AspNetCore.Hosting.StaticWebAssets.StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
builder.Host.UseSerilog();

// Load custom platform settings if they exist
var customSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "Config", "platformsettings.json");
builder.Configuration.AddJsonFile(customSettingsPath, optional: true, reloadOnChange: true);

// Authentication — Cookie for browser users, ApiKey (X-Api-Key header) for external products.
builder.Services.AddHttpContextAccessor();

// Rate limiting — protects login and forgot-password from brute-force attacks
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.AddPolicy("forgotPassword", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddMemoryCache();

var azureAdClientId = builder.Configuration["AzureAd:ClientId"] ?? builder.Configuration["AZURE_AD_CLIENT_ID"];
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"] ?? builder.Configuration["AZURE_AD_CLIENT_SECRET"];
var azureAdTenantId = builder.Configuration["AzureAd:TenantId"] ?? builder.Configuration["AZURE_AD_TENANT_ID"];
var azureAdConfigured = !string.IsNullOrWhiteSpace(azureAdClientId) && !string.IsNullOrWhiteSpace(azureAdTenantId);

var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

if (azureAdConfigured)
{
    authBuilder.AddOpenIdConnect("AzureAd", options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{azureAdTenantId}/v2.0";
        options.ClientId = azureAdClientId!;
        options.ClientSecret = azureAdClientSecret ?? "";
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.ResponseType = "code";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();
                var tenantService = ctx.HttpContext.RequestServices.GetRequiredService<TenantService>();

                var oid = ctx.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                          ?? ctx.Principal?.FindFirst("oid")?.Value;
                var email = ctx.Principal?.FindFirst("preferred_username")?.Value
                            ?? ctx.Principal?.FindFirst("email")?.Value
                            ?? ctx.Principal?.FindFirst("upn")?.Value
                            ?? ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                var name = ctx.Principal?.FindFirst("name")?.Value ?? email;

                if (string.IsNullOrEmpty(email))
                {
                    logger.LogWarning("Azure AD login succeeded but no email claim found for OID {Oid}", oid);
                    ctx.Fail("Unable to identify user from Azure AD claims.");
                    return;
                }

                try
                {
                    var user = await userService.GetByEmailAsync(email);
                    if (user == null)
                    {
                        logger.LogWarning("Azure AD user {Email} not found in MyDesk — access denied", email);
                        ctx.Fail("No matching MyDesk account found for {Email}. Contact your administrator to set up access.".Replace("{Email}", email));
                        return;
                    }

                    var memberships = await tenantService.GetUserTenantsAsync(user.UserId);
                    if (memberships.Count == 0)
                    {
                        logger.LogWarning("Azure AD user {Email} has no tenant memberships", email);
                        ctx.Fail("No workspace access configured. Contact your administrator.");
                        return;
                    }

                    var defaultMembership = memberships.FirstOrDefault(m => m.IsDefault) ?? memberships[0];
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new(ClaimTypes.Name, user.Name),
                        new(ClaimTypes.Email, user.Email),
                        new("tenant_id", defaultMembership.TenantId.ToString()),
                        new("azure_oid", oid ?? ""),
                        new(ClaimTypes.Role, "Administrator"),
                    };

                    var appIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ctx.Principal?.AddIdentity(appIdentity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Azure AD token validation failed for user {Email}", email);
                    ctx.Fail("Failed to resolve MyDesk account. Contact support.");
                }
            },
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ctx.Exception, "Azure AD authentication failed");
                ctx.Response.Redirect("/login?error=azure");
                ctx.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

authBuilder.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, MyDesk.Web.Api.ApiKeyAuthenticationHandler>(
    MyDesk.Web.Api.ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddCascadingAuthenticationState();

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
});

// Database (scoped so each request can carry tenant session context)
builder.Services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<TenantIsolationService>();
builder.Services.AddScoped<MigrationRunnerService>();

// EF Core 10 — scoped DbContext for tenant/identity entities (rest of codebase still
// uses Dapper via DatabaseService; both layers map to the same physical tables).
builder.Services.AddDbContext<MyDeskDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("TechlightDb")));
builder.Services.AddSingleton<NavMenuService>();
builder.Services.AddSingleton<SetupMenuService>();
builder.Services.AddSingleton<EntityColorService>();
builder.Services.AddSingleton<UserPreferencesService>();

// Platform settings service (tenant-aware) — sources from PlatformSettingsEntities table.
// Order: 1) authenticated tenant claim → 2) Request.Host match in TenantHostnames → 3) defaults.
builder.Services.AddScoped<PlatformSettingsService>();

// Auth service (scoped to request)
builder.Services.AddScoped<AuthService>();
// One-time token store for Blazor Server login flow (singleton, in-memory)
builder.Services.AddSingleton<LoginTokenStore>();

// Domain services (all in MyDesk.Shared.Services)
builder.Services.AddScoped<ActivityService>();
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddScoped<EmailService>(sp =>
{
    var db = sp.GetRequiredService<DatabaseService>();
    var activity = sp.GetRequiredService<ActivityService>();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<EmailService>>();
    var platformSettings = sp.GetRequiredService<PlatformSettingsService>().Current;
    var tenantAccessor = sp.GetRequiredService<ICurrentTenantAccessor>();
    // Tenant accessor enables the Demo MyDesk redirect-all-emails-to-peter@bardenhagen.xyz guard.
    return new EmailService(db, activity, config, logger, platformSettings, tenantAccessor);
});
builder.Services.AddScoped<OutlookInboxService>();
builder.Services.AddScoped<PdfService>(sp => 
{
    var db = sp.GetRequiredService<DatabaseService>();
    var logger = sp.GetRequiredService<ILogger<PdfService>>();
    var platformSettings = sp.GetRequiredService<PlatformSettingsService>().Current;
    var svc = new PdfService(db, logger);
    svc.SetSettings(platformSettings);
    return svc;
});
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<SupplierService>(); // Added this
builder.Services.AddScoped<DashboardService>();
builder.Services.AddSingleton<ITargetsProvider, TargetsProvider>();
builder.Services.AddScoped<IntelligenceService>();
builder.Services.Configure<MarketingOptions>(builder.Configuration.GetSection("Marketing"));
builder.Services.AddSingleton<MarketingService>();
builder.Services.AddScoped<MarketingDataService>();
builder.Services.AddScoped<MarketingAIService>();
builder.Services.AddSingleton<MarketingStrategyStore>();
builder.Services.AddScoped<CampaignService>();
// builder.Services.AddSingleton<WeatherOptions>();
// builder.Services.AddHttpClient<WeatherService>();
// builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SystemService>();
builder.Services.AddScoped<BrandAssetService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DespatchService>();
builder.Services.AddScoped<JobOrderService>();
builder.Services.AddScoped<NoticeboardService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<TransporterService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<ErrorLogService>();
builder.Services.AddScoped<StaffWhereaboutsService>();
builder.Services.AddScoped<DRMService>();
builder.Services.AddScoped<BusinessGoalsService>();
builder.Services.AddScoped<ValuationService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<BankingService>();
// builder.Services.AddScoped<NotificationService>(); // Not used - removed
    builder.Services.AddScoped<AuditService>();
    builder.Services.AddScoped<FileLibraryService>();
    builder.Services.AddScoped<FavouritesService>();
    builder.Services.AddScoped<AIFunctionExecutor>();
    builder.Services.AddScoped<FinancialExtractionService>();

    // ── Ports from legacy MyDesk (in-memory services) ──────────────────────
    builder.Services.AddScoped<RfqService>();
    builder.Services.AddScoped<SalesProjectService>();
    builder.Services.AddScoped<CallReportService>();
    builder.Services.AddScoped<PoRequestService>();

    // ── Phase 2 of legacy port: approval chain + sales-reports dashboard ────
    builder.Services.AddScoped<ApprovalService>();
    builder.Services.AddScoped<SalesReportsService>();

builder.Services.AddHttpClient();

// ── Hangfire (background + recurring jobs) ─────────────────────────────────
// Stored in the same SQL DB. Dashboard exposed at /hangfire (admin-only).
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("TechlightDb"), new SqlServerStorageOptions
    {
        SchemaName = "HangFire",
        PrepareSchemaIfNecessary = true,
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
    }));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(2, Environment.ProcessorCount);
    options.Queues = new[] { "default", "scheduled" };
});

// Scheduling layer (uses Hangfire under the hood; keep MyDesk.Shared free of Hangfire deps).
builder.Services.AddScoped<ScheduledTaskService>();
builder.Services.AddScoped<IScheduledTaskRegistrar, ScheduledTaskRegistrar>();
builder.Services.AddScoped<ScheduledTaskExecutor>();

// Demo MyDesk seed (idempotent; runs once on startup if Demo tenant has no demo Companies).
builder.Services.AddSingleton<DemoDataSeeder>();

// ── Ask AI agent + tools ───────────────────────────────────────────────────
// Tools run inside the caller's request scope so they pick up the current
// tenant via ICurrentTenantAccessor — every SQL query is automatically filtered
// by the SQL Row-Level Security policy applied by TenantIsolationService.
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.QuotesSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.InvoicesSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.PipelineSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.ScheduleReportTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.CashFlowForecastTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.SearchComposioAppsTool>();
builder.Services.AddScoped<MyDesk.Web.AI.AskAiAgentService>();

builder.Services.Configure<AzureAIOptions>(builder.Configuration.GetSection(AzureAIOptions.Section));
builder.Services.AddScoped<AzureAIService>();
builder.Services.Configure<MyDesk.Shared.Services.ComposioOptions>(builder.Configuration.GetSection("Composio"));
builder.Services.AddScoped<MyDesk.Shared.Services.ComposioIntegrationService>();
builder.Services.AddScoped<AzureAiVisionClientAdapter>();
builder.Services.AddScoped<MyDesk.Shared.Services.Extraction.IAiVisionClient>(sp =>
    sp.GetRequiredService<AzureAiVisionClientAdapter>());
builder.Services.AddScoped<MyDesk.Shared.Services.Extraction.IDocumentExtractionStrategy, MyDesk.Shared.Services.Extraction.PdfPigExtractionStrategy>();
builder.Services.AddScoped<MyDesk.Shared.Services.Extraction.IDocumentExtractionStrategy, MyDesk.Web.Services.Extraction.DocIntelExtractionStrategy>();
builder.Services.AddScoped<MyDesk.Shared.Services.Extraction.IDocumentExtractionStrategy, MyDesk.Shared.Services.Extraction.GptVisionExtractionStrategy>();
builder.Services.AddScoped<MyDesk.Shared.Services.Extraction.DocumentExtractionService>();
builder.Services.AddScoped<SupplierQuoteParseService>();
builder.Services.AddScoped<McpIntegrationService>();

// Proposal #272: AI Enhancement services
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<AiAuditService>();
builder.Services.AddScoped<TelegramBotService>();
builder.Services.AddHostedService<WorkflowSchedulerService>();

// ── GraphQL (HotChocolate) ─────────────────────────────────────────────────
// Endpoint: /graphql (with Banana Cake Pop UI in Development).
builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<MyDesk.Web.GraphQL.Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

// ── REST API (controllers + OpenAPI / Swagger) ─────────────────────────────
// External products integrate via /api/v1/* using either the cookie session
// (browser clients) or X-Api-Key header (server-to-server).
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MyDesk API",
        Version = "v1",
        Description = "REST API for external products integrating with MyDesk."
    });
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "API key issued to the external product."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Razor Components — DetailedErrors helps developers see what broke without
// having to dig through logs; the circuit handler logs lifecycle events.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        // Don't crater the user's session over a transient JS interop hiccup —
        // give them a chance to reconnect for 3 minutes (default is 3 mins, kept explicit).
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
        options.MaxBufferedUnacknowledgedRenderBatches = 10;
    });
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, ResilientCircuitHandler>();

var app = builder.Build();

// Set QuestPDF community license (free tier, required since v2023.12)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Ensure audit + email log tables exist (idempotent IF NOT EXISTS)
// Wrapped in try/catch so app still starts even if database is temporarily unavailable.
// SystemBypass() sets the SQL session flag so RLS policies don't block startup migrations
// before the policies themselves are even applied.
using (var scope = app.Services.CreateScope())
using (MyDesk.Shared.Services.TenantImpersonation.SystemBypass())
{
    var sp = scope.ServiceProvider;

    // Each EnsureTable call is independently wrapped so one service's failure
    // (e.g. legacy schema mismatch on a single table) does NOT block the rest of
    // startup. In particular, TenantService + PermissionService MUST run or login
    // breaks with a 401 because the user's tenant memberships can't be resolved.
    static async Task SafeInit(string label, Func<Task> work)
    {
        try { await work(); }
        catch (Exception ex) { Log.Warning(ex, "Startup init: '{Label}' failed (continuing)", label); }
    }

    try
    {
        // Migration runner runs FIRST so any *.sql files dropped into Deployment/Migration
        // are applied before downstream EnsureTable services / isolation policies look at the schema.
        await SafeInit("MigrationRunner",          () => sp.GetRequiredService<MigrationRunnerService>().RunPendingAsync(app.Environment.ContentRootPath));

        await SafeInit("ActivityService",          () => sp.GetRequiredService<ActivityService>().EnsureTableAsync());
        await SafeInit("EmailService",             () => sp.GetRequiredService<EmailService>().EnsureTablesAsync());
        await SafeInit("AiAuditService",           () => sp.GetRequiredService<AiAuditService>().EnsureTableAsync());
        await SafeInit("ReportService",            () => sp.GetRequiredService<ReportService>().EnsureTableAsync());
        await SafeInit("LogService",               () => sp.GetRequiredService<LogService>().EnsureTableAsync());
        await SafeInit("ExpenseService",           () => sp.GetRequiredService<ExpenseService>().EnsureTableAsync());
        await SafeInit("ErrorLogService",          () => sp.GetRequiredService<ErrorLogService>().EnsureTableAsync());
        await SafeInit("FinancialExtractionService", () => sp.GetRequiredService<FinancialExtractionService>().EnsureTableAsync());
        await SafeInit("DRMService",               () => sp.GetRequiredService<DRMService>().EnsureTablesAsync());
        await SafeInit("StaffWhereaboutsService",  () => sp.GetRequiredService<StaffWhereaboutsService>().EnsureTableAsync());
        await SafeInit("ProjectService",           () => sp.GetRequiredService<ProjectService>().EnsureTablesAsync());
        await SafeInit("TimesheetService",         () => sp.GetRequiredService<TimesheetService>().EnsureTableAsync());
        await SafeInit("TenantService",            () => sp.GetRequiredService<TenantService>().EnsureTablesAsync());
        await SafeInit("PermissionService",        () => sp.GetRequiredService<PermissionService>().InitializeTableAsync());
        await SafeInit("ScheduledTaskService",     () => sp.GetRequiredService<ScheduledTaskService>().EnsureTablesAsync());
        await SafeInit("BankingService",           () => sp.GetRequiredService<BankingService>().EnsureTablesAsync());
        await SafeInit("FileLibraryService",       () => sp.GetRequiredService<FileLibraryService>().EnsureTableAsync());

        // Back-fill legacy columns absent from pre-v3.0 databases (idempotent — COL_LENGTH guards).
        await SafeInit("Legacy schema backfill", async () =>
        {
            var db2 = sp.GetRequiredService<DatabaseService>();
            await db2.ExecuteNonQueryAsync(@"
                -- JobOrders
                IF OBJECT_ID('JobOrders') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('JobOrders','ContactId')        IS NULL ALTER TABLE JobOrders ADD ContactId        INT NULL;
                    IF COL_LENGTH('JobOrders','JobOrderStatusId') IS NULL ALTER TABLE JobOrders ADD JobOrderStatusId INT NULL DEFAULT 1;
                    IF COL_LENGTH('JobOrders','OriginatorId')     IS NULL ALTER TABLE JobOrders ADD OriginatorId     INT NULL;
                    IF COL_LENGTH('JobOrders','Notes')            IS NULL ALTER TABLE JobOrders ADD Notes            NVARCHAR(MAX) NULL;
                    IF COL_LENGTH('JobOrders','Code')             IS NULL ALTER TABLE JobOrders ADD Code             NVARCHAR(50) NULL;
                END

                -- JobOrderContents
                IF OBJECT_ID('JobOrderContents') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('JobOrderContents','Qty')          IS NULL ALTER TABLE JobOrderContents ADD Qty          DECIMAL(18,4) NULL;
                    IF COL_LENGTH('JobOrderContents','ProductCatId') IS NULL ALTER TABLE JobOrderContents ADD ProductCatId INT NULL;
                    IF COL_LENGTH('JobOrderContents','Price')        IS NULL ALTER TABLE JobOrderContents ADD Price        DECIMAL(18,4) NULL;
                END

                -- Expenses
                IF OBJECT_ID('Expenses') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Expenses','Amount')     IS NULL ALTER TABLE Expenses ADD Amount     DECIMAL(18,2) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('Expenses','Total')      IS NULL ALTER TABLE Expenses ADD Total      DECIMAL(18,2) NOT NULL DEFAULT 0;
                    IF COL_LENGTH('Expenses','SupplierId') IS NULL ALTER TABLE Expenses ADD SupplierId INT NULL;
                    IF COL_LENGTH('Expenses','Category')   IS NULL ALTER TABLE Expenses ADD Category   NVARCHAR(100) NOT NULL DEFAULT 'General';
                    IF COL_LENGTH('Expenses','Status')     IS NULL ALTER TABLE Expenses ADD Status     NVARCHAR(50)  NOT NULL DEFAULT 'Pending';
                    IF COL_LENGTH('Expenses','FileName')   IS NULL ALTER TABLE Expenses ADD FileName   NVARCHAR(500) NULL;
                END

                -- Noticeboard
                IF OBJECT_ID('Noticeboard') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Noticeboard','Title')      IS NULL ALTER TABLE Noticeboard ADD Title      NVARCHAR(200)  NULL;
                    IF COL_LENGTH('Noticeboard','Notice')     IS NULL ALTER TABLE Noticeboard ADD Notice     NVARCHAR(MAX)  NULL;
                    IF COL_LENGTH('Noticeboard','DatePosted') IS NULL ALTER TABLE Noticeboard ADD DatePosted DATETIME       NULL DEFAULT GETDATE();
                    IF COL_LENGTH('Noticeboard','ExpiryDate') IS NULL ALTER TABLE Noticeboard ADD ExpiryDate DATETIME       NULL;
                    IF COL_LENGTH('Noticeboard','PostedBy')   IS NULL ALTER TABLE Noticeboard ADD PostedBy   NVARCHAR(100)  NULL;
                END");
        });

        Log.Information("Database tables verified successfully");

        // Apply tenant isolation: NOT NULL TenantId + DEFAULT(SESSION_CONTEXT) +
        // SQL Row-Level Security policies on every tenant-scoped table.
        await SafeInit("TenantIsolationService",   () => sp.GetRequiredService<TenantIsolationService>().EnforceAsync());

        // Idempotent demo data — only seeds rows if Demo MyDesk has none yet.
        // The seeder uses TenantImpersonation.For(DemoTenantId) so RLS allows its writes.
        await SafeInit("DemoDataSeeder",           () => app.Services.GetRequiredService<DemoDataSeeder>().SeedAsync());
    }
    catch (Exception ex)
    {
        // SafeInit eats individual failures; this catch only fires for completely
        // unexpected things (e.g. DI resolution failure).
        Log.Warning(ex, "Database initialization aborted unexpectedly - app will start but some features may be unavailable. Check connection string in appsettings.json");
    }
}

// Request logging with enriched properties (dev: Debug, prod: Information)
app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (ctx, elapsed, ex) =>
        ex != null ? LogEventLevel.Error
        : ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
        : ctx.Response.StatusCode >= 400 ? LogEventLevel.Warning
        : app.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RemoteIP", http.Connection.RemoteIpAddress?.ToString() ?? "-");
        diag.Set("User", http.User?.Identity?.Name ?? "-");
        diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
    };
});

// Global exception handler - logs EVERY unhandled request exception
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error != null)
        {
            Log.Error(feature.Error,
                "Unhandled exception at {Path} for user {User}",
                feature.Path, ctx.User?.Identity?.Name ?? "anonymous");

            // Log to database
            try
            {
                var errorLogSvc = ctx.RequestServices.GetService<ErrorLogService>();
                if (errorLogSvc != null)
                {
                    await errorLogSvc.LogErrorAsync(new MyDesk.Shared.Models.ErrorLog
                    {
                        ErrorDate = DateTime.Now,
                        Severity = "Error",
                        ExceptionType = feature.Error.GetType().Name,
                        Message = feature.Error.Message,
                        StackTrace = feature.Error.StackTrace,
                        InnerException = feature.Error.InnerException?.Message,
                        RequestUrl = feature.Path,
                        HttpMethod = ctx.Request.Method,
                        UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                        IPAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                        UserId = ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        UserName = ctx.User?.Identity?.Name,
                        Source = "GlobalExceptionHandler"
                    });
                }
            }
            catch
            {
                // If DB logging fails, we already logged to Serilog above
            }
        }
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        if (ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = "Internal Server Error", message = app.Environment.IsDevelopment() ? feature?.Error?.Message : "An unexpected error occurred." });
        }
        else
        {
            ctx.Response.Redirect("/Error");
        }
    });
});

app.UseStaticFiles();
app.MapStaticAssets(); // .NET 8+ optimized static file delivery

// ── Robots.txt - Block ALL search engine indexing ──────────────────────────
app.MapGet("/robots.txt", (HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/plain";
    return Results.Text(@"User-agent: *
Disallow: /
Disallow: /login
Disallow: /dashboard
Disallow: /quotes
Disallow: /invoices
Disallow: /purchase-orders
Disallow: /contacts
Disallow: /companies
Disallow: /products
Disallow: /despatch
Disallow: /reports
Disallow: /reconciliation
Disallow: /marketing
Disallow: /admin
Disallow: /settings
Disallow: /profile
Disallow: /files
Disallow: /ask-ai
Disallow: /activity
Disallow: /favourites
Disallow: /calendar
Disallow: /expenses
Disallow: /integrations
Disallow: /accounting
Disallow: /job-orders
Disallow: /noticeboard
Disallow: /help
Disallow: /customer-portal
Disallow: /supplier-portal
Disallow: /api/
Sitemap: ", contentType: "text/plain");
});

// ── X-Robots-Tag middleware - Prevent indexing of ALL pages ────────────────
app.Use(async (ctx, next) =>
{
    // Never allow search engines to index any page
    ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet, noimageindex";
    ctx.Response.Headers["Cache-Control"] = ctx.Request.Path.StartsWithSegments("/api/marketing") 
        ? "private, no-store, max-age=0" 
        : "no-store, no-cache, must-revalidate";
    
    // Security headers
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-XSS-Protection"] = "0"; // Modern browsers use CSP instead
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // CSP: Allow self, inline scripts/styles for Blazor, Google Fonts, and MudBlazor
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: blob:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self';";
    
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// HSTS only in production (development uses HTTP)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── GraphQL endpoint ────────────────────────────────────────────────────────
app.MapGraphQL("/graphql");

// ── REST API + OpenAPI ──────────────────────────────────────────────────────
app.MapControllers();

// Swagger only in development — leak of API schema in production is a security risk
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyDesk API v1");
        c.RoutePrefix = "swagger";
    });
}

// ── Hangfire dashboard (admin/director only) ───────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyDesk.Web.Scheduling.HangfireAdminAuthorizationFilter() }
});

// ── Blazor Server sign-in endpoint (one-time-token pattern) ─────────────────
// Blazor components cannot set cookies (response already started over SignalR).
// Login.razor validates credentials, stores a 30-second token, then navigates
// here (forceLoad). This endpoint sets the auth cookie and redirects to the app.
app.MapGet("/auth/signin", async (HttpContext ctx, LoginTokenStore tokenStore, UserService userSvc, TenantService tenantSvc, AuthService auth) =>
{
    var token = ctx.Request.Query["token"].ToString();
    var returnUrl = ctx.Request.Query["returnUrl"].ToString();
    var tenantIdRaw = ctx.Request.Query["tenantId"].ToString();
    if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/")) returnUrl = "/";

    if (string.IsNullOrEmpty(token))
    {
        Log.Warning("/auth/signin called with no token");
        return Results.Redirect("/login?error=1");
    }

    var peek = tokenStore.PeekToken(token);
    if (peek == null)
    {
        Log.Warning("/auth/signin: token not found or expired");
        return Results.Redirect("/login?error=1");
    }

    var user = await userSvc.GetAsync(peek.Value.UserId);
    if (user == null)
    {
        Log.Warning("/auth/signin: user {UserId} not found", peek.Value.UserId);
        return Results.Redirect("/login?error=1");
    }

    await tenantSvc.EnsureUserTenantAssignmentsAsync();
    var memberships = await tenantSvc.GetUserTenantsAsync(user.UserId);
    if (memberships.Count == 0)
    {
        Log.Warning("/auth/signin: user {UserId} has no tenant memberships", user.UserId);
        return Results.Redirect("/login?error=1");
    }

    var selectedMembership = memberships.Count == 1 ? memberships[0] : null as TenantMembership;
    if (memberships.Count > 1)
    {
        if (!Guid.TryParse(tenantIdRaw, out var selectedTenantId))
        {
            return Results.Redirect($"/login/select-tenant?token={Uri.EscapeDataString(token)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        selectedMembership = memberships.FirstOrDefault(m => m.TenantId == selectedTenantId);
        if (selectedMembership is null)
        {
            Log.Warning("/auth/signin: user {UserId} tried unauthorized tenant {TenantId}", user.UserId, tenantIdRaw);
            return Results.Redirect("/login?error=1");
        }
    }

    if (selectedMembership is null)
    {
        Log.Warning("/auth/signin: no tenant membership resolved for user {UserId}", user.UserId);
        return Results.Redirect("/login?error=1");
    }

    var entry = tokenStore.ConsumeToken(token);
    if (entry == null)
    {
        Log.Warning("/auth/signin: token expired before consume");
        return Results.Redirect("/login?error=1");
    }

    Log.Information("Login SUCCESS via token: UserId={UserId} Code={Code} Name={Name} Tenant={Tenant} (RememberMe={RememberMe})",
        user.UserId, user.Code, user.Name, selectedMembership.TenantName, entry.Value.RememberMe);
    await auth.SignInAsync(ctx, user, selectedMembership, entry.Value.RememberMe);
    return Results.Redirect(returnUrl);
});

// Legacy POST endpoint (kept for compatibility)
app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth, LoginTokenStore tokenStore) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var login = form["login"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "on";

    var user = await auth.ValidateLoginAsync(login, password);
    if (user != null)
    {
        Log.Information("Login SUCCESS (POST): {Login} -> UserId={UserId} Code={Code} Name={Name}",
            login, user.UserId, user.Code, user.Name);
        var token = tokenStore.CreateToken(user.UserId, user.Code, rememberMe);
        return Results.Redirect($"/auth/signin?token={token}&returnUrl=%2F");
    }
    Log.Warning("Login FAILED for {Login} from {RemoteIP}", login, ctx.Connection.RemoteIpAddress);
    return Results.Redirect("/login?error=1");
}).RequireRateLimiting("login");

// Forgot password endpoint
app.MapPost("/api/auth/forgot-password", async (HttpContext ctx, EmailService emailSvc) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();

    Log.Information("Password reset requested for {Email} from {RemoteIP}",
        email, ctx.Connection.RemoteIpAddress);

    try
    {
        // Generate a reset token and send email
        var resetToken = Guid.NewGuid().ToString("N");
        var resetLink = $"{ctx.Request.Scheme}://{ctx.Request.Host}/reset-password?token={resetToken}";
        
        // Send password reset email
        await emailSvc.SendPasswordResetEmailAsync(email, resetLink);
        
        Log.Information("Password reset email sent to {Email}", email);
        return Results.Redirect("/forgot-password?success=1");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to send password reset email to {Email}", email);
        return Results.Redirect("/forgot-password?error=1");
    }
}).RequireRateLimiting("forgotPassword");

// ── PDF Download endpoints (authenticated — uses existing session cookie) ──────
app.MapGet("/api/pdf/quote/{id:int}", async (int id, PdfService pdfSvc) =>
{
    try
    {
        var bytes = await pdfSvc.GenerateQuotePdfAsync(id);
        return Results.File(bytes, "application/pdf", $"Quote-{id}.pdf");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "PDF generation failed for quote {Id}", id);
        return Results.Problem($"Could not generate PDF: {ex.Message}");
    }
}).RequireAuthorization();

app.MapGet("/api/pdf/invoice/{id:int}", async (int id, PdfService pdfSvc) =>
{
    try
    {
        var bytes = await pdfSvc.GenerateInvoicePdfAsync(id);
        return Results.File(bytes, "application/pdf", $"Invoice-{id}.pdf");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "PDF generation failed for invoice {Id}", id);
        return Results.Problem($"Could not generate PDF: {ex.Message}");
    }
}).RequireAuthorization();

app.MapGet("/api/pdf/purchase-order/{id:int}", async (int id, PdfService pdfSvc) =>
{
    try
    {
        var bytes = await pdfSvc.GeneratePurchaseOrderPdfAsync(id);
        return Results.File(bytes, "application/pdf", $"PurchaseOrder-{id}.pdf");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "PDF generation failed for PO {Id}", id);
        return Results.Problem($"Could not generate PDF: {ex.Message}");
    }
}).RequireAuthorization();

app.MapGet("/api/pdf/despatch/{id:int}", async (int id, PdfService pdfSvc) =>
{
    try
    {
        var bytes = await pdfSvc.GenerateDespatchPdfAsync(id);
        return Results.File(bytes, "application/pdf", $"Despatch-{id}.pdf");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "PDF generation failed for despatch {Id}", id);
        return Results.Problem($"Could not generate PDF: {ex.Message}");
    }
}).RequireAuthorization();

// ── Marketing asset endpoints (authenticated, X-Robots-Tag: noindex) ────────
app.MapGet("/api/marketing/download/{key}/{label}", (string key, string label, HttpContext ctx, MarketingService svc) =>
{
    ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet";
    ctx.Response.Headers["Cache-Control"] = "private, no-store, max-age=0";

    var file = svc.GetFile(key, label);
    if (file == null || !File.Exists(file.FullPath))
        return Results.NotFound();

    var mime = GetMimeType(file.FullPath);
    var filename = Path.GetFileName(file.FullPath);
    return Results.File(file.FullPath, mime, filename, enableRangeProcessing: false);
}).RequireAuthorization();

// Preview endpoint — serves brand logos/images inline (not for download)
app.MapGet("/api/marketing/preview", (string path, HttpContext ctx, IOptions<MarketingOptions> opts) =>
{
    ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet, noimageindex";
    ctx.Response.Headers["Cache-Control"] = "private, no-store, max-age=0";

    // Only allow paths inside the configured AssetsRoot (prevent traversal)
    var root = opts.Value.AssetsRoot;
    if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return Results.NotFound();

    var rel = path.TrimStart('/').Replace("..", "").Replace('\\', '/');
    var full = Path.GetFullPath(Path.Combine(root, rel));
    if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();
    if (!File.Exists(full)) return Results.NotFound();

    return Results.File(full, GetMimeType(full));
}).RequireAuthorization();

static string GetMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".svg"  => "image/svg+xml",
    ".png"  => "image/png",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".pdf"  => "application/pdf",
    ".doc"  => "application/msword",
    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ".html" or ".htm" => "text/html",
    _        => "application/octet-stream"
};

// ── Brand Assets API endpoints ─────────────────────────────────────────────
app.MapGet("/api/brand-assets/download/{id:guid}", (Guid id, BrandAssetService svc) =>
{
    var asset = svc.GetAsset(id);
    if (asset == null) return Results.NotFound();
    
    var path = svc.GetFullPath(asset);
    if (!File.Exists(path)) return Results.NotFound();
    
    var mime = BrandAssetService.GetMimeType(asset.FileName);
    return Results.File(path, mime, asset.OriginalFileName);
}).RequireAuthorization();

app.MapPost("/api/brand-assets/upload", async (HttpContext ctx, BrandAssetService svc) =>
{
    if (!ctx.User.IsInRole("Admin") && !ctx.User.IsInRole("Director"))
        return Results.Forbid();
    
    var form = await ctx.Request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null) return Results.BadRequest("No file uploaded");
    
    // Validate file size (max 10MB)
    const long maxFileSize = 10 * 1024 * 1024;
    if (file.Length > maxFileSize)
        return Results.BadRequest("File too large. Maximum size is 10MB.");
    
    // Validate file type
    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
        return Results.BadRequest($"File type not allowed. Allowed: {string.Join(", ", allowedExtensions)}");
    
    // Validate MIME type
    var allowedMimeTypes = new[] { "image/png", "image/jpeg", "image/gif", "image/svg+xml", "application/pdf", 
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
    if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        return Results.BadRequest("Invalid file content type.");
    
    var category = form["category"].ToString();
    var description = form["description"].ToString();
    var uploadedBy = ctx.User.Identity?.Name ?? "Unknown";
    
    using var stream = file.OpenReadStream();
    var browserFile = new BrowserFileWrapper(file.FileName, file.ContentType, file.Length, stream);
    var asset = await svc.UploadAsync(browserFile, category, description, uploadedBy);
    
    return Results.Ok(new { asset.Id, asset.OriginalFileName, asset.FileSize });
}).RequireAuthorization();

app.MapDelete("/api/brand-assets/{id:guid}", async (Guid id, HttpContext ctx, BrandAssetService svc) =>
{
    if (!ctx.User.IsInRole("Admin") && !ctx.User.IsInRole("Director"))
        return Results.Forbid();
    
    var deletedBy = ctx.User.Identity?.Name ?? "Unknown";
    await svc.DeleteAsync(id, deletedBy);
    return Results.Ok();
}).RequireAuthorization();

// ── Email endpoints ─────────────────────────────────────────────────────────
app.MapPost("/api/email/quote/{id:int}",
    async (int id, HttpContext ctx, EmailRequest req, PdfService pdfSvc, EmailService emailSvc) =>
{
    var senderCode = ctx.User.Identity?.Name ?? "SYSTEM";
    byte[]? pdf = null;
    if (req.AttachPdf)
    {
        try { pdf = await pdfSvc.GenerateQuotePdfAsync(id); } catch { }
    }
    var ok = await emailSvc.EmailQuoteAsync(id, req.To, req.Subject, req.Message, senderCode, pdf);
    return ok ? Results.Ok(new { sent = true }) : Results.Problem("Email send failed — check SMTP config.");
}).RequireAuthorization();

app.MapPost("/api/email/invoice/{id:int}",
    async (int id, HttpContext ctx, EmailRequest req, PdfService pdfSvc, EmailService emailSvc) =>
{
    var senderCode = ctx.User.Identity?.Name ?? "SYSTEM";
    byte[]? pdf = null;
    if (req.AttachPdf)
    {
        try { pdf = await pdfSvc.GenerateInvoicePdfAsync(id); } catch { }
    }
    var ok = await emailSvc.EmailInvoiceAsync(id, req.To, req.Subject, req.Message, senderCode, pdf);
    return ok ? Results.Ok(new { sent = true }) : Results.Problem("Email send failed — check SMTP config.");
}).RequireAuthorization();

app.MapPost("/api/email/purchase-order/{id:int}",
    async (int id, HttpContext ctx, EmailRequest req, PdfService pdfSvc, EmailService emailSvc) =>
{
    var senderCode = ctx.User.Identity?.Name ?? "SYSTEM";
    byte[]? pdf = null;
    if (req.AttachPdf)
    {
        try { pdf = await pdfSvc.GeneratePurchaseOrderPdfAsync(id); } catch { }
    }
    var ok = await emailSvc.EmailPurchaseOrderAsync(id, req.To, req.Subject, req.Message, senderCode, pdf);
    return ok ? Results.Ok(new { sent = true }) : Results.Problem("Email send failed — check SMTP config.");
}).RequireAuthorization();

// ── Telegram Bot webhook (Proposal #272) ─────────────────────────────────────
app.MapPost("/api/telegram/webhook", async (HttpRequest request, TelegramBotService tg) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var doc = System.Text.Json.JsonDocument.Parse(body);
        await tg.HandleUpdateAsync(doc.RootElement);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Telegram webhook error");
        return Results.Ok(); // Always 200 to Telegram
    }
});

// ── Log Viewer API endpoints ─────────────────────────────────────────────────
app.MapGet("/api/logs", (HttpContext ctx) =>
{
    if (!ctx.User.IsInRole("Admin") && !ctx.User.IsInRole("Director"))
        return Results.Forbid();

    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
    if (!Directory.Exists(logsDir))
        return Results.Ok("[]");

    var logs = new List<object>();
    foreach (var file in Directory.GetFiles(logsDir, "*.log").OrderByDescending(f => f))
    {
        try
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(new[] { " [" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var timestamp = parts[0].Trim();
                    var levelPart = parts[1].Trim().TrimEnd(']');
                    var level = levelPart switch
                    {
                        "INF" => "Information",
                        "WRN" => "Warning",
                        "ERR" => "Error",
                        "DBG" => "Debug",
                        _ => "Information"
                    };
                    var message = string.Join(" [", parts.Skip(2)).Trim();
                    logs.Add(new { Timestamp = timestamp, Level = level, Source = "MyDesk.Web", Message = message, Exception = "" });
                }
            }
        }
        catch { }
    }
    return Results.Text(System.Text.Json.JsonSerializer.Serialize(logs), "application/json");
}).RequireAuthorization();

app.MapDelete("/api/logs", (HttpContext ctx) =>
{
    if (!ctx.User.IsInRole("Admin") && !ctx.User.IsInRole("Director"))
        return Results.Forbid();

    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
    if (Directory.Exists(logsDir))
    {
        foreach (var file in Directory.GetFiles(logsDir, "*.log"))
        {
            try { File.Delete(file); } catch { }
        }
    }
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    Log.Information("Logout: {User}", ctx.User?.Identity?.Name ?? "anonymous");
    var hasAzureSession = ctx.User?.Claims.Any(c => c.Type == "azure_oid") ?? false;
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (hasAzureSession && azureAdConfigured)
    {
        var props = new AuthenticationProperties { RedirectUri = "/login" };
        await ctx.SignOutAsync("AzureAd", props);
        return Results.Empty;
    }
    return Results.Redirect("/login");
});

app.MapGet("/integrations/microsoft/connect", (HttpContext ctx, PlatformSettingsService settingsSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false))
        return Results.Redirect("/login");

    var settings = settingsSvc.Current;
    var cfg = settings.MyOutlook;

    if (string.IsNullOrWhiteSpace(cfg.ClientId) || string.IsNullOrWhiteSpace(cfg.ClientSecret))
        return Results.Redirect("/integrations?error=microsoft-not-configured");

    var userCode = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(userCode))
        return Results.Redirect("/integrations?error=user-not-found");

    var redirectUri = !string.IsNullOrWhiteSpace(cfg.RedirectUri)
        ? cfg.RedirectUri!
        : $"{ctx.Request.Scheme}://{ctx.Request.Host}/integrations/microsoft/callback";

    var scope = Uri.EscapeDataString("offline_access openid profile User.Read Mail.Read Mail.ReadWrite Mail.Send Calendars.ReadWrite Files.ReadWrite");
    var statePayload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userCode}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
    var authUrl =
        $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={Uri.EscapeDataString(cfg.ClientId!)}" +
        $"&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_mode=query&scope={scope}" +
        $"&state={Uri.EscapeDataString(statePayload)}&prompt=select_account";

    return Results.Redirect(authUrl);
}).RequireAuthorization();

app.MapGet("/integrations/microsoft/callback", async (HttpContext ctx, PlatformSettingsService settingsSvc, IHttpClientFactory httpFactory) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false))
        return Results.Redirect("/login");

    var code = ctx.Request.Query["code"].ToString();
    var state = ctx.Request.Query["state"].ToString();
    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        return Results.Redirect("/integrations?error=missing-auth-code");

    var settings = settingsSvc.Current;
    var cfg = settings.MyOutlook;
    if (string.IsNullOrWhiteSpace(cfg.ClientId) || string.IsNullOrWhiteSpace(cfg.ClientSecret))
        return Results.Redirect("/integrations?error=microsoft-not-configured");

    string stateUserCode;
    try
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(state)));
        stateUserCode = decoded.Split('|', StringSplitOptions.RemoveEmptyEntries)[0];
    }
    catch
    {
        return Results.Redirect("/integrations?error=invalid-state");
    }

    var userCode = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    if (!string.Equals(userCode, stateUserCode, StringComparison.OrdinalIgnoreCase))
        return Results.Redirect("/integrations?error=state-user-mismatch");

    var redirectUri = !string.IsNullOrWhiteSpace(cfg.RedirectUri)
        ? cfg.RedirectUri!
        : $"{ctx.Request.Scheme}://{ctx.Request.Host}/integrations/microsoft/callback";

    var form = new Dictionary<string, string>
    {
        ["client_id"] = cfg.ClientId!,
        ["client_secret"] = cfg.ClientSecret!,
        ["grant_type"] = "authorization_code",
        ["code"] = code,
        ["redirect_uri"] = redirectUri,
        ["scope"] = "offline_access openid profile User.Read Mail.Read Mail.ReadWrite Mail.Send Calendars.ReadWrite Files.ReadWrite"
    };

    var http = httpFactory.CreateClient();
    using var tokenResponse = await http.PostAsync(
        "https://login.microsoftonline.com/common/oauth2/v2.0/token",
        new FormUrlEncodedContent(form));

    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
    if (!tokenResponse.IsSuccessStatusCode)
    {
        Log.Warning("Microsoft token exchange failed: {Status} {Body}", tokenResponse.StatusCode, tokenJson);
        return Results.Redirect("/integrations?error=token-exchange-failed");
    }

    using var tokenDoc = JsonDocument.Parse(tokenJson);
    var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var a) ? a.GetString() : null;
    var refreshToken = tokenDoc.RootElement.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
    var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;

    if (string.IsNullOrWhiteSpace(accessToken))
        return Results.Redirect("/integrations?error=missing-access-token");

    if (!settings.MyOutlookUserConnections.TryGetValue(userCode, out var conn))
    {
        conn = new IntegrationSettings();
    }

    conn.Enabled = true;
    conn.IsConnected = true;
    conn.AccessToken = accessToken;
    conn.RefreshToken = refreshToken;
    conn.TokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));
    conn.LastSyncTime = DateTime.UtcNow;
    conn.Status = "Connected";
    conn.SyncCalendar = conn.SyncCalendar || settings.MyOutlook.SyncCalendar;
    conn.SyncContacts = conn.SyncContacts || settings.MyOutlook.SyncContacts;
    conn.SyncEmail = conn.SyncEmail || settings.MyOutlook.SyncEmail;
    conn.SyncDrive = true;

    settings.MyOutlookUserConnections[userCode] = conn;
    await settingsSvc.SaveAsync();

    return Results.Redirect("/integrations?connected=microsoft");
}).RequireAuthorization();

Log.Information("Application configured. Environment={Env}, URLs={Urls}",
    app.Environment.EnvironmentName, string.Join(", ", app.Urls.DefaultIfEmpty("default")));

// ── Quote Action endpoints (Email approve/decline) ───────────────────────────
app.MapGet("/quotes/{id:int}/action/{action}", async (int id, string action, QuoteService quoteSvc, ActivityService activitySvc) =>
{
    var quote = await quoteSvc.GetQuoteAsync(id);
    if (quote == null) return Results.NotFound("Quote not found");

    string resultMessage = "";

    // 1. Validate status: Don't let customer click Decline after Accepted
    if (quote.QuoteStatusId == 4 && action.Equals("decline", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Cannot decline an already accepted quote.");
    }

    if (action.Equals("approve", StringComparison.OrdinalIgnoreCase))
    {
        if (quote.QuoteStatusId == 4) return Results.BadRequest("Quote is already accepted.");
        await quoteSvc.UpdateStatusAsync(id, 4, "Accepted via Email", "Customer");
        await activitySvc.LogAsync("Customer", "Quote", id, "Accepted via Email", quote.Reference);
        resultMessage = "Quote has been accepted successfully.";
    }
    else if (action.Equals("decline", StringComparison.OrdinalIgnoreCase))
    {
        await quoteSvc.UpdateStatusAsync(id, 10, "Declined via Email", "Customer");
        await activitySvc.LogAsync("Customer", "Quote", id, "Declined via Email", quote.Reference);
        resultMessage = "Quote has been declined.";
    }
    else
    {
        return Results.BadRequest("Invalid action");
    }

    // Return a simple HTML response
    return Results.Content($@"<html><body style='font-family:sans-serif;text-align:center;padding:50px;'><h1>{resultMessage}</h1><p>Thank you.</p></body></html>", "text/html");
});

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.Information("Application shutting down");
    Log.CloseAndFlush();
}

/// <summary>Body model for POST /api/email/* endpoints.</summary>
public record EmailRequest(
    string To,
    string? Subject    = null,
    string? Message    = null,
    bool    AttachPdf  = true);
