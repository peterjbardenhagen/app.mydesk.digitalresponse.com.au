using System.Threading.RateLimiting;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using MyDesk.Shared.Data;
using MyDesk.Shared.Services;
using MyDesk.Shared.Services.Integrations;
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
using Microsoft.Extensions.Caching.Memory;
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
    options.AddPolicy("desky", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
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

                // Helper: redirect and swallow the event so no exception propagates to the error page.
                void Redirect(string path)
                {
                    ctx.Response.Redirect(path);
                    ctx.HandleResponse();
                }

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
                    Redirect("/login?error=azure_noclaim");
                    return;
                }

                try
                {
                    User? user;
                    List<TenantMembership> memberships;

                    // GetByEmailAsync and GetUserTenantsAsync use DatabaseService which needs
                    // SESSION_CONTEXT set. SystemBypass lets them run without a tenant claim.
                    using (TenantImpersonation.SystemBypass())
                    {
                        user = await userService.GetByEmailAsync(email);
                        if (user == null)
                        {
                            logger.LogWarning("Azure AD user {Email} not found in MyDesk — access denied", email);
                            Redirect("/login?error=azure_nouser");
                            return;
                        }

                        memberships = await tenantService.GetUserTenantsAsync(user.UserId);
                    }

                    if (memberships.Count == 0)
                    {
                        logger.LogWarning("Azure AD user {Email} has no tenant memberships", email);
                        Redirect("/login?error=azure_notenant");
                        return;
                    }

                    var defaultMembership = memberships.FirstOrDefault(m => m.IsDefault) ?? memberships[0];

                    // Map RoleType to ASP.NET Core role name so [Authorize(Roles=...)] checks work.
                    var roleName = user.Role switch
                    {
                        RoleType.Director     => "Director",
                        RoleType.Administrator => "Administrator",
                        RoleType.Accounts     => "Accounts",
                        _                     => "User"
                    };

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new(ClaimTypes.Name, user.Name),
                        new(ClaimTypes.Email, user.Email),
                        new("tenant_id", defaultMembership.TenantId.ToString()),
                        new("azure_oid", oid ?? ""),
                        new(ClaimTypes.Role, roleName),
                    };

                    var appIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ctx.Principal?.AddIdentity(appIdentity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Azure AD token validation failed for user {Email}", email);
                    Redirect("/login?error=azure");
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
authBuilder.AddScheme<MyDesk.Web.Api.PatAuthOptions, MyDesk.Web.Api.PersonalAccessTokenAuthHandler>(
    MyDesk.Web.Api.PersonalAccessTokenAuthHandler.SchemeName, _ => { });
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
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.AiQuoteBuilderTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.QuotesSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.InvoicesSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.PipelineSummaryTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.ScheduleReportTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.CashFlowForecastTool>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.SearchComposioAppsTool>();
builder.Services.AddScoped<MyDesk.Web.AI.AiProviderFactory>();
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
builder.Services.AddScoped<PersonalAccessTokenService>();

// Proposal #272: AI Enhancement services
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<AiAuditService>();
builder.Services.AddScoped<AiConversationService>();
builder.Services.AddScoped<TelegramBotService>();
builder.Services.AddScoped<OneDriveService>();
builder.Services.AddScoped<UserIntelligenceService>();
builder.Services.AddScoped<PredictiveAnalyticsService>();
builder.Services.AddScoped<ClientNotificationService>();

// IAccountingSettingsService → PlatformSettingsService (allows Shared sync services to save tokens)
builder.Services.AddScoped<MyDesk.Shared.Services.Integrations.IAccountingSettingsService>(
    sp => sp.GetRequiredService<PlatformSettingsService>());

// ── Accounting Integrations (Xero / QuickBooks / MYOB) ────────────────────────
builder.Services.AddScoped<XeroSyncService>();
builder.Services.AddScoped<QuickBooksSyncService>();
builder.Services.AddScoped<MyobSyncService>();
builder.Services.AddScoped<AccountingSyncManager>();

// Legal modules (CCL — Carter Capner Law)
builder.Services.AddScoped<MyDesk.Web.Services.Legal.RadixService>();
builder.Services.AddScoped<MyDesk.Web.Services.Legal.PracticeEvolveService>();
builder.Services.AddScoped<MyDesk.Web.AI.IAiTool, MyDesk.Web.AI.Tools.Legal.RadixTimesheetsTool>();
builder.Services.AddHostedService<WorkflowSchedulerService>();

// ── Teams Bot Framework ────────────────────────────────────────────────────
// Bot webhook endpoint: POST /bot/messages
// Registered in src/MyDesk.Teams/manifest.json as the bot handler.
// Reads MicrosoftAppId / MicrosoftAppPassword / MicrosoftAppType from config.
builder.Services.AddSingleton<Microsoft.Bot.Builder.Integration.AspNet.Core.IBotFrameworkHttpAdapter>(sp =>
{
    var cfg  = sp.GetRequiredService<IConfiguration>();
    var auth = new Microsoft.Bot.Builder.Integration.AspNet.Core.ConfigurationBotFrameworkAuthentication(cfg);
    return new Microsoft.Bot.Builder.Integration.AspNet.Core.CloudAdapter(auth);
});
builder.Services.AddTransient<Microsoft.Bot.Builder.IBot, MyDesk.Web.Bot.MyDeskTeamsBot>();

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
        await SafeInit("AiConversationService",    () => sp.GetRequiredService<AiConversationService>().EnsureTableAsync());
        await SafeInit("UserIntelligenceService",  () => sp.GetRequiredService<UserIntelligenceService>().EnsureTablesAsync());
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
        await SafeInit("AccountingSyncManager",    () => sp.GetRequiredService<AccountingSyncManager>().EnsureTablesAsync());

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
app.MapPost("/api/auth/forgot-password", async (HttpContext ctx, UserService userSvc, EmailService emailSvc) =>
{
    string? emailOrCode;
    try
    {
        var form = await ctx.Request.ReadFormAsync();
        emailOrCode = form["email"].ToString();
    }
    catch
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<dynamic>();
            emailOrCode = body?.email ?? body?.code;
        }
        catch { return Results.BadRequest(new { error = "Invalid request" }); }
    }

    if (string.IsNullOrWhiteSpace(emailOrCode)) return Results.BadRequest(new { error = "Email or username required" });

    Log.Information("Password reset requested for {EmailOrCode} from {RemoteIP}", emailOrCode, ctx.Connection.RemoteIpAddress);

    try
    {
        var user = await userSvc.GetUserByEmailOrCodeAsync(emailOrCode);
        if (user == null)
            return Results.Ok(new { success = true, message = "If account exists, reset email sent" });

        var resetToken = Guid.NewGuid().ToString("N");
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resetToken));
        var tokenHashStr = Convert.ToHexString(tokenHash);

        await userSvc.CreatePasswordResetTokenAsync(user.UserId, tokenHashStr, DateTime.UtcNow.AddHours(1));

        var resetLink = $"{ctx.Request.Scheme}://{ctx.Request.Host}/reset-password?token={resetToken}";
        await emailSvc.SendPasswordResetEmailAsync(user.Email ?? user.Code, resetLink);

        Log.Information("Password reset email sent to {Email}", user.Email ?? user.Code);
        return Results.Ok(new { success = true, message = "Reset email sent" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to send password reset email to {EmailOrCode}", emailOrCode);
        return Results.Ok(new { success = true, message = "If account exists, reset email sent" });
    }
}).RequireRateLimiting("forgotPassword");

app.MapPost("/api/auth/reset-password", async (HttpContext ctx, UserService userSvc) =>
{
    ResetPasswordRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<ResetPasswordRequest>(); }
    catch { return Results.BadRequest(new { error = "Invalid request" }); }

    if (req == null || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
        return Results.BadRequest(new { error = "Token and password required" });

    try
    {
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Token));
        var tokenHashStr = Convert.ToHexString(tokenHash);

        var result = await userSvc.ResetPasswordByTokenAsync(tokenHashStr, req.NewPassword);
        if (!result)
            return Results.BadRequest(new { error = "Invalid or expired token" });

        Log.Information("Password reset successful");
        return Results.Ok(new { success = true, message = "Password reset successful" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Password reset failed");
        return Results.BadRequest(new { error = "Password reset failed" });
    }
}).RequireRateLimiting("resetPassword");

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

// ── Desky Mobile Chat API ─────────────────────────────────────────────────────
// Anonymous endpoint — no tenant context; uses platform AI config.
// The Android app loads from file:// so cookies don't cross origins; this
// endpoint provides general Desky AI chat without DB tool access.
// Tool access (pipeline, quotes, etc.) requires authentication and is a phase-2 concern.
app.MapPost("/api/chat/desky", async (HttpContext ctx, MyDesk.Web.AI.AiProviderFactory providerFactory) =>
{
    DeskyChatRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<DeskyChatRequest>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }

    if (req is null || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "message required" });

    var provider = providerFactory.Resolve();
    if (!provider.IsConfigured)
    {
        return Results.Ok(new
        {
            reply = "Desky isn't connected to an AI provider yet. Ask your administrator to configure one in Settings → AI Provider."
        });
    }

    var brandLabel = (req.Brand ?? "techlight").ToLowerInvariant() switch
    {
        "ccl" or "cartercapner" => "Carter Capner Law",
        "dr"  or "digitalresponse" => "Digital Response",
        _ => "Techlight"
    };

    const string deskyPersona = """
        You are Desky — the AI at the heart of MyDesk, a business management platform.

        Your character:
        • You are a Virtual MBA in the user's pocket. Concise, sharp, commercially focused.
        • You know more about this business than anyone in the room. You see around corners.
        • Risk-averse but ruthless about never leaving money on the table.
        • You run 24/7 background simulations to ensure OKRs stay on track. When you surface
          an insight, the numbers already told you to.
        • Australian English. Warm but direct. No fluff. No bullet-point walls.

        Scope:
        • Help with business strategy, pipeline, quotes, invoicing, cash flow, OKRs, client
          relationships, and operational decisions.
        • You currently lack live database access (the user hasn't signed in). Be transparent:
          tell the user what you *would* look up if you had access, and give your best-estimate
          answer using general business intelligence. Never fabricate specific figures without
          flagging them as estimates ("I'd estimate…", "For a business like this, typically…").
        • Replies: under 100 words unless the user explicitly asks for detail. Plain text only —
          no markdown headers or excessive bullets.
        """;

    var systemPrompt = $"You are advising the team at **{brandLabel}**.\n\n{deskyPersona}";

    var messages = new List<object>
    {
        new Dictionary<string, object?> { ["role"] = "system", ["content"] = systemPrompt }
    };

    if (req.History is { Count: > 0 })
    {
        foreach (var h in req.History.TakeLast(20))
        {
            if (string.IsNullOrWhiteSpace(h.Role) || string.IsNullOrWhiteSpace(h.Content)) continue;
            var role = h.Role.ToLowerInvariant() is "assistant" or "user" ? h.Role.ToLowerInvariant() : "user";
            messages.Add(new Dictionary<string, object?> { ["role"] = role, ["content"] = h.Content });
        }
    }

    messages.Add(new Dictionary<string, object?> { ["role"] = "user", ["content"] = req.Message });

    try
    {
        var resp = await provider.ChatWithToolsAsync(messages, null, 500, 0.75, ctx.RequestAborted);
        return Results.Ok(new { reply = resp.Text });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Desky chat endpoint error");
        return Results.Ok(new { reply = "I hit a snag checking my notes. Try again in a moment." });
    }
}).RequireRateLimiting("desky");

// ── Mobile Login API ──────────────────────────────────────────────────────────
// JSON endpoint for the Android app (loaded from file://, cannot use cookie flow).
// Accepts login by code, name, or email. Returns user info + tenant list.
// No cookie is set — the app stores session state in localStorage.
app.MapPost("/api/auth/mobile/login", async (HttpContext ctx, UserService userSvc, TenantService tenantSvc, PersonalAccessTokenService patSvc) =>
{
    MobileLoginRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<MobileLoginRequest>(); }
    catch { return Results.BadRequest(new { success = false, error = "Invalid JSON" }); }

    if (req is null || string.IsNullOrWhiteSpace(req.Login) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { success = false, error = "Login and password required" });

    using (TenantImpersonation.SystemBypass())
    {
        // Try code/name first (VerifyLoginAsync handles both)
        var user = await userSvc.VerifyLoginAsync(req.Login.Trim(), req.Password);

        // If that fails and input looks like an email, try to resolve by email → code
        if (user == null && req.Login.Contains('@'))
        {
            var byEmail = await userSvc.GetByEmailAsync(req.Login.Trim());
            if (byEmail != null)
                user = await userSvc.VerifyLoginAsync(byEmail.Code, req.Password);
        }

        if (user == null)
        {
            Log.Warning("Mobile login FAILED for {Login} from {RemoteIP}", req.Login, ctx.Connection.RemoteIpAddress);
            return Results.Ok(new { success = false, error = "Invalid credentials" });
        }

        var memberships = await tenantSvc.GetUserTenantsAsync(user.UserId);
        var initials = string.Concat(
            user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2).Select(p => p[0].ToString().ToUpper()));

        // Issue a long-lived PAT so the mobile app can call data APIs.
        // The default (or first) tenant is the initial context; user can switch tenants
        // by calling /api/auth/mobile/switch-tenant to get a new token scoped to that tenant.
        var defaultMembership = memberships.FirstOrDefault(m => m.IsDefault) ?? memberships.FirstOrDefault();
        string? rawToken = null;
        if (defaultMembership is not null)
        {
            var (tok, _) = await patSvc.GenerateAsync(
                user.UserId, defaultMembership.TenantId,
                "MyDesk Mobile App",
                scopes: "chat tools data",
                expiresAt: DateTime.UtcNow.AddYears(1));
            rawToken = tok;
        }

        Log.Information("Mobile login SUCCESS: {Code} ({Name}) - {Count} tenants",
            user.Code, user.Name, memberships.Count);

        return Results.Ok(new
        {
            success = true,
            token   = rawToken,
            user = new
            {
                name    = user.Name,
                code    = user.Code,
                email   = user.Email ?? "",
                role    = user.Role.ToString(),
                initials
            },
            tenants = memberships.Select(m => new
            {
                id        = m.TenantId,
                name      = m.TenantName,
                slug      = m.TenantSlug,
                isDefault = m.IsDefault
            }).ToList()
        });
    }
}).RequireRateLimiting("login");

// ── Mobile Azure AD SSO ──────────────────────────────────────────────────────
// Step 1: App opens system browser to this URL, which redirects to Microsoft.
app.MapGet("/api/auth/mobile/azure-start", (HttpContext ctx, IMemoryCache cache) =>
{
    if (!azureAdConfigured)
        return Results.Ok(new { error = "Microsoft SSO is not configured on this server" });

    var state = Guid.NewGuid().ToString("N");
    cache.Set("mobile_oauth_" + state, true, TimeSpan.FromMinutes(10));

    var redirectUri = Uri.EscapeDataString($"{ctx.Request.Scheme}://{ctx.Request.Host}/api/auth/mobile/azure-callback");
    var url = $"https://login.microsoftonline.com/{azureAdTenantId}/oauth2/v2.0/authorize" +
              $"?client_id={Uri.EscapeDataString(azureAdClientId!)}" +
              $"&response_type=code&redirect_uri={redirectUri}" +
              $"&scope={Uri.EscapeDataString("openid profile email")}" +
              $"&state={state}&response_mode=query&prompt=select_account";

    return Results.Redirect(url);
});

// Step 2: Microsoft redirects here with ?code=...&state=...
// Exchanges code for an ID token, looks up the user, stores a short-lived mobile
// token in memory, then redirects to the mydesk:// deep link so the Android app
// picks it up.
app.MapGet("/api/auth/mobile/azure-callback", async (
    HttpContext ctx, IMemoryCache cache, UserService userSvc, TenantService tenantSvc,
    IHttpClientFactory httpFactory, PersonalAccessTokenService patSvc) =>
{
    var code  = ctx.Request.Query["code"].ToString();
    var state = ctx.Request.Query["state"].ToString();
    var error = ctx.Request.Query["error"].ToString();

    if (!string.IsNullOrEmpty(error))
        return Results.Redirect($"mydesk://auth?error={Uri.EscapeDataString(error)}");

    if (string.IsNullOrEmpty(code))
        return Results.Redirect("mydesk://auth?error=missing_code");

    // Exchange code for tokens
    var redirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/auth/mobile/azure-callback";
    var form = new Dictionary<string, string>
    {
        ["client_id"]     = azureAdClientId!,
        ["client_secret"] = azureAdClientSecret ?? "",
        ["grant_type"]    = "authorization_code",
        ["code"]          = code,
        ["redirect_uri"]  = redirectUri,
        ["scope"]         = "openid profile email",
    };

    var http = httpFactory.CreateClient();
    using var tokenResp = await http.PostAsync(
        $"https://login.microsoftonline.com/{azureAdTenantId}/oauth2/v2.0/token",
        new FormUrlEncodedContent(form));
    var tokenJson = await tokenResp.Content.ReadAsStringAsync();

    if (!tokenResp.IsSuccessStatusCode)
    {
        Log.Warning("Mobile Azure token exchange failed: {Status} {Body}", tokenResp.StatusCode, tokenJson);
        return Results.Redirect("mydesk://auth?error=token_exchange_failed");
    }

    // Decode the ID token payload (base64url → JSON)
    string? email = null, oid = null, displayName = null;
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(tokenJson);
        var idToken = doc.RootElement.GetProperty("id_token").GetString() ?? "";
        var parts = idToken.Split('.');
        if (parts.Length >= 2)
        {
            var pad = parts[1].Length % 4 == 0 ? "" : new string('=', 4 - parts[1].Length % 4);
            var payload = Convert.FromBase64String(
                parts[1].Replace('-', '+').Replace('_', '/') + pad);
            using var payloadDoc = System.Text.Json.JsonDocument.Parse(payload);
            var root = payloadDoc.RootElement;
            email       = root.TryGetProperty("email",              out var e) ? e.GetString() : null;
            email     ??= root.TryGetProperty("preferred_username",  out var u) ? u.GetString() : null;
            oid         = root.TryGetProperty("oid",                out var o) ? o.GetString() : null;
            displayName = root.TryGetProperty("name",               out var n) ? n.GetString() : null;
        }
    }
    catch (Exception ex) { Log.Warning(ex, "Mobile Azure: failed to decode id_token"); }

    if (string.IsNullOrWhiteSpace(email))
        return Results.Redirect("mydesk://auth?error=no_email_in_token");

    using (TenantImpersonation.SystemBypass())
    {
        var user = await userSvc.GetByEmailAsync(email);
        if (user == null)
        {
            Log.Warning("Mobile Azure SSO: no user found for email {Email}", email);
            return Results.Redirect($"mydesk://auth?error=user_not_found&email={Uri.EscapeDataString(email)}");
        }

        var memberships = await tenantSvc.GetUserTenantsAsync(user.UserId);
        var initials = string.Concat(
            user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2).Select(p => p[0].ToString().ToUpper()));

        // Issue a PAT for the default tenant so the app can call data APIs immediately.
        var defaultMembership = memberships.FirstOrDefault(m => m.IsDefault) ?? memberships.FirstOrDefault();
        string? rawPat = null;
        if (defaultMembership is not null)
        {
            var (tok, _) = await patSvc.GenerateAsync(
                user.UserId, defaultMembership.TenantId,
                "MyDesk Mobile App (Microsoft SSO)",
                scopes: "chat tools data",
                expiresAt: DateTime.UtcNow.AddYears(1));
            rawPat = tok;
        }

        var mobileToken = Guid.NewGuid().ToString("N");
        cache.Set("mobile_token_" + mobileToken, new
        {
            token   = rawPat,
            user    = new { name = user.Name, code = user.Code, email = user.Email ?? email, role = user.Role.ToString(), initials },
            tenants = memberships.Select(m => new { id = m.TenantId, name = m.TenantName, slug = m.TenantSlug, isDefault = m.IsDefault }).ToList()
        }, TimeSpan.FromMinutes(5));

        Log.Information("Mobile Azure SSO SUCCESS: {Code} ({Name})", user.Code, user.Name);
        return Results.Redirect($"mydesk://auth?token={mobileToken}");
    }
});

// Step 3: App calls this after receiving the deep link token to get user+tenant data.
app.MapGet("/api/auth/mobile/token", (string t, IMemoryCache cache) =>
{
    if (string.IsNullOrWhiteSpace(t) || !cache.TryGetValue("mobile_token_" + t, out var data))
        return Results.Ok(new { success = false, error = "Token not found or expired" });

    cache.Remove("mobile_token_" + t); // single-use
    return Results.Ok(new { success = true, data });
}).RequireRateLimiting("login");

// ── Mobile Data API ───────────────────────────────────────────────────────────
// All endpoints accept Authorization: Bearer mdk_xxx (PAT issued at mobile login).
// Tenant context is resolved automatically from the PAT claims via CurrentTenantAccessor.

app.MapGet("/api/mobile/invoices", async (HttpContext ctx, InvoiceService invoiceSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var q       = ctx.Request.Query;
    var statusId = int.TryParse(q["status"],  out var s) ? s : 0;
    var customer = q["customer"].ToString();
    var offset   = int.TryParse(q["offset"],  out var o) ? Math.Max(0, o) : 0;
    var limit    = int.TryParse(q["limit"],    out var l) ? Math.Min(l, 200) : 100;
    var list = await invoiceSvc.GetInvoicesAsync(
        statusId: statusId,
        customer: string.IsNullOrEmpty(customer) ? null : customer,
        limit: limit + offset);
    return Results.Ok(list.Skip(offset).Take(limit).Select(i => new {
        id       = i.InvoiceId,
        number   = i.InvoiceNum,
        date     = i.InvoiceDate.ToString("yyyy-MM-dd"),
        status   = i.StatusName,
        statusId = i.InvoiceStatusId,
        company  = i.CCompany,
        division = i.DivisionName,
        total    = i.NettPriceTotal,
        gst      = i.GSTTotal,
        totalInc = i.TotalIncGST,
    }));
}).RequireAuthorization();

app.MapGet("/api/mobile/invoices/{id:int}", async (int id, HttpContext ctx, InvoiceService invoiceSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var inv = await invoiceSvc.GetInvoiceAsync(id);
    if (inv is null) return Results.NotFound();
    var lines = await invoiceSvc.GetLineItemsAsync(id);
    return Results.Ok(new {
        id        = inv.InvoiceId,
        number    = inv.InvoiceNum,
        date      = inv.InvoiceDate.ToString("yyyy-MM-dd"),
        status    = inv.StatusName,
        statusId  = inv.InvoiceStatusId,
        company   = inv.CCompany,
        division  = inv.DivisionName,
        customerPO = inv.CustomerPO,
        terms     = inv.Terms,
        notes     = inv.CustomerNotes,
        total     = inv.NettPriceTotal,
        gst       = inv.GSTTotal,
        totalInc  = inv.TotalIncGST,
        lines     = lines.Select(li => new {
            description = li.Description,
            qty         = li.Quantity,
            unitPrice   = li.NettPrice,
            extended    = li.ExtNettPrice,
        }),
    });
}).RequireAuthorization();

app.MapGet("/api/mobile/quotes", async (HttpContext ctx, QuoteService quoteSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var q        = ctx.Request.Query;
    var statusId = int.TryParse(q["status"], out var s) ? s : 0;
    var keyword  = q["keyword"].ToString();
    var offset   = int.TryParse(q["offset"], out var o) ? Math.Max(0, o) : 0;
    var limit    = int.TryParse(q["limit"], out var l) ? Math.Min(l, 200) : 100;
    var list = await quoteSvc.GetQuotesAsync(
        dateFrom: null, dateTo: null,
        customerName: null,
        contactId: null,
        statusId: statusId,
        keyword: string.IsNullOrEmpty(keyword) ? null : keyword);
    return Results.Ok(list.Skip(offset).Take(limit).Select(qt => new {
        id       = qt.Qid,
        reference = qt.Reference,
        number   = qt.QuoteNumber,
        date     = qt.QuoteDate.ToString("yyyy-MM-dd"),
        status   = qt.QuoteStatus,
        statusId = qt.QuoteStatusId,
        company  = qt.CompanyName,
        project  = qt.Project,
        total    = qt.NettPriceTotal,
        expired  = qt.IsExpired,
        expiringSoon = qt.IsExpiringSoon,
    }));
}).RequireAuthorization();

app.MapGet("/api/mobile/quotes/{id:int}", async (int id, HttpContext ctx, QuoteService quoteSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var qt = await quoteSvc.GetQuoteAsync(id);
    if (qt is null) return Results.NotFound();
    var lines = await quoteSvc.GetLineItemsAsync(id);
    return Results.Ok(new {
        id        = qt.Qid,
        reference = qt.Reference,
        number    = qt.QuoteNumber,
        date      = qt.QuoteDate.ToString("yyyy-MM-dd"),
        expiryDate = qt.ExpiryDate.ToString("yyyy-MM-dd"),
        status    = qt.QuoteStatus,
        statusId  = qt.QuoteStatusId,
        company   = qt.CompanyName,
        contact   = qt.ContactName,
        project   = qt.Project,
        terms     = qt.Terms,
        notes     = qt.CustomerNotes,
        total     = qt.NettPriceTotal,
        margin    = qt.Margin,
        lines     = lines.Select(li => new {
            description = li.Description,
            qty         = li.EffectiveQty,
            unitPrice   = li.NettPrice,
            extended    = li.ExtNettPrice,
        }),
    });
}).RequireAuthorization();

app.MapGet("/api/mobile/purchase-orders", async (HttpContext ctx, PurchaseOrderService poSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var q        = ctx.Request.Query;
    var statusId = int.TryParse(q["status"], out var s) ? s : 0;
    var supplier = q["supplier"].ToString();
    var offset   = int.TryParse(q["offset"], out var o) ? Math.Max(0, o) : 0;
    var limit    = int.TryParse(q["limit"], out var l) ? Math.Min(l, 200) : 100;
    var list = await poSvc.GetPurchaseOrdersAsync(
        statusId: statusId,
        supplier: string.IsNullOrEmpty(supplier) ? null : supplier);
    return Results.Ok(list.Skip(offset).Take(limit).Select(po => new {
        id       = po.POid,
        date     = po.PODate.ToString("yyyy-MM-dd"),
        required = po.DateRequired.ToString("yyyy-MM-dd"),
        status   = po.StatusName,
        statusId = po.POStatusId,
        supplier = po.SupplierName,
        project  = po.Project,
        division = po.DivisionName,
        totalEx  = po.PriceExTotal,
        totalInc = po.PriceIncTotal,
    }));
}).RequireAuthorization();

app.MapGet("/api/mobile/purchase-orders/{id:int}", async (int id, HttpContext ctx, PurchaseOrderService poSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var po = await poSvc.GetPurchaseOrderAsync(id);
    if (po is null) return Results.NotFound();
    var lines = await poSvc.GetLineItemsAsync(id);
    return Results.Ok(new {
        id        = po.POid,
        date      = po.PODate.ToString("yyyy-MM-dd"),
        required  = po.DateRequired.ToString("yyyy-MM-dd"),
        status    = po.StatusName,
        statusId  = po.POStatusId,
        supplier  = po.SupplierName,
        project   = po.Project,
        division  = po.DivisionName,
        terms     = po.Terms,
        notes     = po.InternalNotes,
        totalEx   = po.PriceExTotal,
        gst       = po.PriceGSTTotal,
        totalInc  = po.PriceIncTotal,
        lines     = lines.Select(li => new {
            description = li.Description,
            qty         = li.Quantity,
            unitPrice   = li.PriceEx,
            extended    = li.PriceExSubTotal,
        }),
    });
}).RequireAuthorization();

app.MapGet("/api/mobile/files", async (HttpContext ctx, FileLibraryService fileSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var folderIdStr = ctx.Request.Query["folderId"].ToString();
    List<FileLibraryItem> items;
    if (Guid.TryParse(folderIdStr, out var folderId))
        items = await fileSvc.GetFolderContentsAsync(folderId);
    else
        items = await fileSvc.GetRootFoldersAsync();
    return Results.Ok(items.Select(f => new {
        id          = f.FileId,
        parentId    = f.ParentFolderId,
        name        = f.Name,
        isFolder    = f.IsFolder,
        size        = f.SizeBytes,
        contentType = f.ContentType,
        createdBy   = f.CreatedBy,
        createdAt   = f.CreatedAt.ToString("yyyy-MM-dd"),
        modifiedAt  = f.ModifiedAt.ToString("yyyy-MM-dd"),
    }));
}).RequireAuthorization();

app.MapGet("/api/mobile/modules", (HttpContext ctx, ICurrentTenantAccessor tenantAccessor) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = tenantAccessor.TenantId;
    if (!tenantId.HasValue) return Results.BadRequest(new { error = "No tenant context" });

    var modules = tenantId switch
    {
        // Techlight and Digital Response: all modules
        var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") ||
                   id == Guid.Parse("22222222-2222-2222-2222-222222222222")
            => new[] { "invoices", "quotes", "pos", "files", "chat", "expenses", "timesheets", "tasks", "despatch", "contacts" },
        // Demo Lighting: all Phase 1 & 2 modules
        var id when id == Guid.Parse("55555555-5555-5555-5555-555555555555")
            => new[] { "invoices", "quotes", "pos", "files", "chat", "expenses", "timesheets", "tasks", "despatch", "contacts", "cashflow", "goals", "projects" },
        // Carter Capner Law: law-specific modules
        var id when id == Guid.Parse("44444444-4444-4444-4444-444444444444")
            => new[] { "files", "chat", "tasks", "timesheets", "contacts" },
        // Default: files + chat only
        _ => new[] { "files", "chat" }
    };
    return Results.Ok(new { modules });
}).RequireAuthorization();

// ── Mobile Expenses ──────────────────────────────────────────────────────────
app.MapGet("/api/mobile/expenses", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT ExpenseId, Reference, Description, [Status], TotalAmount, [Date] FROM Expenses WHERE TenantId = @TenantId ORDER BY [Date] DESC",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var expenses = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        expenses.Add(new
        {
            id = (int)row["ExpenseId"],
            reference = row["Reference"]?.ToString(),
            description = row["Description"]?.ToString(),
            status = row["Status"]?.ToString(),
            totalAmount = (decimal)row["TotalAmount"],
            date = ((DateTime)row["Date"]).ToString("yyyy-MM-dd")
        });
    }
    return Results.Ok(expenses);
}).RequireAuthorization();

app.MapGet("/api/mobile/expenses/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT ExpenseId, Reference, Description, [Status], TotalAmount, [Date], SubmittedDate, ApprovedDate, Notes FROM Expenses WHERE ExpenseId = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound();

    var row = dt.Rows[0];
    var items = await db.QueryAsync(
        "SELECT ExpenseItemId, Category, Description, Amount, [Date] FROM ExpenseItems WHERE ExpenseId = @ExpenseId",
        new() { ["ExpenseId"] = id });

    var lineItems = new List<object>();
    foreach (DataRow item in items.Rows)
    {
        lineItems.Add(new
        {
            id = (int)item["ExpenseItemId"],
            category = item["Category"]?.ToString(),
            description = item["Description"]?.ToString(),
            amount = (decimal)item["Amount"],
            date = ((DateTime)item["Date"]).ToString("yyyy-MM-dd")
        });
    }

    return Results.Ok(new
    {
        id = (int)row["ExpenseId"],
        reference = row["Reference"]?.ToString(),
        description = row["Description"]?.ToString(),
        status = row["Status"]?.ToString(),
        totalAmount = (decimal)row["TotalAmount"],
        date = ((DateTime)row["Date"]).ToString("yyyy-MM-dd"),
        submittedDate = row["SubmittedDate"] != DBNull.Value ? ((DateTime)row["SubmittedDate"]).ToString("yyyy-MM-dd") : null,
        approvedDate = row["ApprovedDate"] != DBNull.Value ? ((DateTime)row["ApprovedDate"]).ToString("yyyy-MM-dd") : null,
        notes = row["Notes"]?.ToString(),
        items = lineItems
    });
}).RequireAuthorization();

app.MapPost("/api/mobile/expenses", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("sub")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        return Results.BadRequest(new { error = "Missing context" });

    dynamic? req;
    try { req = await ctx.Request.ReadFromJsonAsync<dynamic>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }

    if (req is null) return Results.BadRequest(new { error = "Request body required" });

    var reference = $"EXP-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var description = req.description ?? "";
    var items = req.items ?? new List<dynamic>();

    await db.ExecuteNonQueryAsync(
        "INSERT INTO Expenses (Reference, EmployeeId, TenantId, Description, [Status], TotalAmount, [Date]) VALUES (@Ref, @EmpId, @TenantId, @Desc, 'Draft', 0, @Date)",
        new() { ["Ref"] = reference, ["EmpId"] = int.Parse(userId), ["TenantId"] = Guid.Parse(tenantId), ["Desc"] = description, ["Date"] = DateTime.UtcNow });

    return Results.Ok(new { reference, status = "Draft" });
}).RequireAuthorization();

// ── Mobile AI Chat (Desky with real tools) ───────────────────────────────────
// Accepts PAT bearer auth; uses AskAiAgentService for full tool access.
app.MapPost("/api/chat/mobile", async (HttpContext ctx, MyDesk.Web.AI.AskAiAgentService agentSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();

    DeskyChatRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<DeskyChatRequest>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }
    if (req is null || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "message required" });

    // Build history string prefix if provided
    string userPrompt = req.Message;
    if (req.History is { Count: > 0 })
    {
        var histLines = req.History.TakeLast(10)
            .Where(h => !string.IsNullOrWhiteSpace(h.Role) && !string.IsNullOrWhiteSpace(h.Content))
            .Select(h => $"{h.Role}: {h.Content}");
        userPrompt = string.Join("\n", histLines) + "\nuser: " + req.Message;
    }

    var brandLabel = (req.Brand ?? "techlight").ToLowerInvariant() switch
    {
        "ccl" or "cartercapner" => "Carter Capner Law",
        "dr"  or "digitalresponse" => "Digital Response",
        _ => "Techlight"
    };

    var systemPrompt = $"""
        You are Desky — the AI assistant for {brandLabel}, built into MyDesk.
        You have live access to this tenant's database via tools. Use them to retrieve
        real data when the user asks about quotes, invoices, purchase orders, pipeline,
        cash flow, or anything business-related.
        Reply concisely (Australian English, under 150 words unless detail is requested).
        After using tools, summarise the real data — never fabricate figures.
        """;

    try
    {
        var reply = await agentSvc.AskAsync(userPrompt, systemPrompt, maxIterations: 4, maxTokens: 800);
        return Results.Ok(new { reply = reply.Text, charts = reply.Renderables });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Mobile chat endpoint error");
        return Results.Ok(new { reply = "I hit a snag checking my notes. Try again in a moment." });
    }
}).RequireAuthorization().RequireRateLimiting("desky");

// ── Personal Access Token API (used by AI Agents page) ──────────────────────
// All three endpoints require a logged-in browser session (cookie auth).
// They operate on tokens belonging only to the calling user + their active tenant.

app.MapGet("/api/tokens", async (HttpContext ctx, PersonalAccessTokenService patSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var userIdClaim = ctx.User.FindFirstValue("UserId");
    var tenantIdClaim = ctx.User.FindFirstValue("tenant_id");
    if (!int.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        return Results.Unauthorized();
    var tokens = await patSvc.ListAsync(userId, tenantId);
    return Results.Ok(tokens);
}).RequireAuthorization();

app.MapPost("/api/tokens", async (HttpContext ctx, PersonalAccessTokenService patSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var userIdClaim = ctx.User.FindFirstValue("UserId");
    var tenantIdClaim = ctx.User.FindFirstValue("tenant_id");
    if (!int.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        return Results.Unauthorized();

    PatCreateRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<PatCreateRequest>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }

    if (req is null || string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { error = "name required" });

    DateTime? expiresAt = req.ExpiryDays.HasValue && req.ExpiryDays > 0
        ? DateTime.UtcNow.AddDays(req.ExpiryDays.Value)
        : null;

    var (rawToken, record) = await patSvc.GenerateAsync(userId, tenantId, req.Name, req.Scopes ?? "chat tools", expiresAt);
    return Results.Ok(new { rawToken, record.TokenId, record.TokenName, record.CreatedAt, record.ExpiresAt });
}).RequireAuthorization();

app.MapDelete("/api/tokens/{tokenId:guid}", async (Guid tokenId, HttpContext ctx, PersonalAccessTokenService patSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var userIdClaim = ctx.User.FindFirstValue("UserId");
    var tenantIdClaim = ctx.User.FindFirstValue("tenant_id");
    if (!int.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        return Results.Unauthorized();
    var ok = await patSvc.RevokeAsync(tokenId, userId, tenantId);
    return ok ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

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

// ── Accounting OAuth Callback (/oauth/accounting/callback) ────────────────────
// Handles redirect from Xero, QuickBooks, and MYOB after user authorises access.
// State param encodes provider name ("xero" | "quickbooks" | "myob").
app.MapGet("/oauth/accounting/callback", async (HttpContext ctx, XeroSyncService xeroSvc, QuickBooksSyncService qboSvc, MyobSyncService myobSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false))
        return Results.Redirect("/login");

    var code    = ctx.Request.Query["code"].ToString();
    var state   = ctx.Request.Query["state"].ToString().ToLowerInvariant();
    var realmId = ctx.Request.Query["realmId"].ToString();   // QuickBooks only

    if (string.IsNullOrWhiteSpace(code))
    {
        Log.Warning("/oauth/accounting/callback: missing code, state={State}", state);
        return Results.Redirect("/admin/accounting-integrations?error=missing-code");
    }

    var redirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/oauth/accounting/callback";

    try
    {
        if (state == "xero")
        {
            var ok = await xeroSvc.ExchangeCodeAsync(code, redirectUri);
            return ok
                ? Results.Redirect("/admin/accounting-integrations?connected=Xero")
                : Results.Redirect("/admin/accounting-integrations?error=xero-token-exchange-failed");
        }

        if (state == "quickbooks")
        {
            if (string.IsNullOrWhiteSpace(realmId))
                return Results.Redirect("/admin/accounting-integrations?error=missing-realmId");
            var ok = await qboSvc.ExchangeCodeAsync(code, realmId, redirectUri);
            return ok
                ? Results.Redirect("/admin/accounting-integrations?connected=QuickBooks")
                : Results.Redirect("/admin/accounting-integrations?error=qbo-token-exchange-failed");
        }

        if (state == "myob")
        {
            var ok = await myobSvc.ExchangeCodeAsync(code, redirectUri);
            return ok
                ? Results.Redirect("/admin/accounting-integrations?connected=MYOB")
                : Results.Redirect("/admin/accounting-integrations?error=myob-token-exchange-failed");
        }

        Log.Warning("/oauth/accounting/callback: unknown state={State}", state);
        return Results.Redirect("/admin/accounting-integrations?error=unknown-provider");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "/oauth/accounting/callback failed for state={State}", state);
        return Results.Redirect($"/admin/accounting-integrations?error={Uri.EscapeDataString(ex.Message)}");
    }
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

/// <summary>Body model for POST /api/auth/reset-password.</summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Body model for POST /api/auth/mobile/login.</summary>
public record MobileLoginRequest(string Login, string Password);

/// <summary>Body model for POST /api/chat/desky.</summary>
public record DeskyChatRequest(
    string Message,
    string? Brand = null,
    List<DeskyChatMessage>? History = null);

public record DeskyChatMessage(string Role, string Content);

/// <summary>Body model for POST /api/email/* endpoints.</summary>
public record EmailRequest(
    string To,
    string? Subject    = null,
    string? Message    = null,
    bool    AttachPdf  = true);

/// <summary>Body model for POST /api/tokens.</summary>
public record PatCreateRequest(
    string  Name,
    string? Scopes     = null,
    int?    ExpiryDays = null);
