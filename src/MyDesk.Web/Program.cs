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
using NotificationService = MyDesk.Web.Services.NotificationService;
using ClientNotificationService = MyDesk.Web.Services.ClientNotificationService;
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
using System.Data;
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
    // builder.Services.AddScoped<WorkflowApprovalService>();
    // builder.Services.AddScoped<PhotoProcessingService>();
    // builder.Services.AddScoped<NotificationService>();

    // ── Security Services (Phase 1 Week 4) ──────────────────────────────────
    // builder.Services.AddScoped<RateLimitingService>();

    // ── Ports from legacy MyDesk (in-memory services) ──────────────────────
    builder.Services.AddScoped<RfqService>();
    builder.Services.AddScoped<SalesProjectService>();
    builder.Services.AddScoped<CallReportService>();
    builder.Services.AddScoped<PoRequestService>();

    // ── Phase 2 of legacy port: approval chain + sales-reports dashboard ────
    builder.Services.AddScoped<ApprovalService>();
    builder.Services.AddScoped<SalesReportsService>();

    // ── Phase 5: Notifications & Alerts (2026) ──────────────────────────────
    // Registered before Phase 4 so BudgetService can depend on BudgetAlertService
    builder.Services.AddScoped<NotificationAuditService>();
    builder.Services.AddScoped<NotificationService>();
    builder.Services.AddScoped<ApprovalNotificationService>();
    builder.Services.AddScoped<BudgetAlertService>();
    builder.Services.AddScoped<NotificationRetryService>();
    builder.Services.AddSingleton<NotificationBackgroundJobService>();

    // ── Phase 4: Teams & Departments (2026) ────────────────────────────────
    builder.Services.AddScoped<DepartmentService>();
    builder.Services.AddScoped<TeamService>();
    builder.Services.AddScoped<BudgetService>();
    builder.Services.AddScoped<ApprovalDelegationService>();
    builder.Services.AddScoped<ApprovalEscalationService>();
    builder.Services.AddScoped<BulkUserImportService>();

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
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<DashboardExportService>();
builder.Services.AddScoped<DashboardChartService>();
builder.Services.AddScoped<DashboardReportScheduleService>();

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

// Phase 1 Week 4: Database-backed rate limiting middleware - DISABLED
// app.UseMiddleware<MyDesk.Web.Middleware.RateLimitingMiddleware>();

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

// ── Initialize recurring notification jobs ──────────────────────────────────
var jobService = app.Services.GetRequiredService<NotificationBackgroundJobService>();
jobService.RegisterRecurringJobs();

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
app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth, LoginTokenStore tokenStore, DatabaseService db) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var login = form["login"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "on";

    // Domain-based tenant resolution (Phase 1 Week 1)
    // If login is an email, extract domain and resolve to tenant
    int? resolvedTenantId = null;
    if (login.Contains("@"))
    {
        string emailDomain = login.Substring(login.IndexOf("@") + 1).ToLower();
        var domainResult = await db.QueryAsync(
            @"SELECT TenantId FROM dbo.TenantDomains
              WHERE Domain = @Domain AND IsActive = 1 AND IsVerified = 1",
            new() { ["Domain"] = emailDomain });

        if (domainResult.Rows.Count > 0)
        {
            resolvedTenantId = (int)domainResult.Rows[0]["TenantId"];
            Log.Information("Domain-based tenant resolution: {Email} -> TenantId={TenantId}", login, resolvedTenantId);
        }
    }

    var user = await auth.ValidateLoginAsync(login, password);
    if (user != null)
    {
        // If domain was resolved, verify user belongs to that tenant
        if (resolvedTenantId.HasValue)
        {
            var userTenants = await db.QueryAsync(
                "SELECT TenantId FROM dbo.UserTenants WHERE UserId = @UserId AND TenantId = @TenantId",
                new() { ["UserId"] = user.UserId, ["TenantId"] = resolvedTenantId.Value });

            if (userTenants.Rows.Count == 0)
            {
                Log.Warning("Login FAILED (tenant mismatch): {Login} - user not member of resolved tenant", login);
                return Results.Redirect("/login?error=1&reason=tenant_mismatch");
            }
        }

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

// ── Mobile Timesheets ────────────────────────────────────────────────────────
app.MapGet("/api/mobile/timesheets", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 50 TimesheetId, Reference, [Status], TotalHours, WeekStartDate FROM Timesheets WHERE TenantId = @TenantId ORDER BY WeekStartDate DESC",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var timesheets = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        timesheets.Add(new
        {
            id = (int)row["TimesheetId"],
            reference = row["Reference"]?.ToString(),
            status = row["Status"]?.ToString(),
            totalHours = (decimal)row["TotalHours"],
            weekStartDate = ((DateTime)row["WeekStartDate"]).ToString("yyyy-MM-dd")
        });
    }
    return Results.Ok(timesheets);
}).RequireAuthorization();

app.MapGet("/api/mobile/timesheets/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TimesheetId, Reference, [Status], TotalHours, WeekStartDate, SubmittedDate, ApprovedDate, Notes FROM Timesheets WHERE TimesheetId = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound();

    var row = dt.Rows[0];
    var entries = await db.QueryAsync(
        "SELECT EntryId, [Date], ProjectName, [Description], Hours FROM TimesheetEntries WHERE TimesheetId = @TimesheetId ORDER BY [Date]",
        new() { ["TimesheetId"] = id });

    var lineItems = new List<object>();
    foreach (DataRow entry in entries.Rows)
    {
        lineItems.Add(new
        {
            id = (int)entry["EntryId"],
            date = ((DateTime)entry["Date"]).ToString("yyyy-MM-dd"),
            projectName = entry["ProjectName"]?.ToString(),
            description = entry["Description"]?.ToString(),
            hours = (decimal)entry["Hours"]
        });
    }

    return Results.Ok(new
    {
        id = (int)row["TimesheetId"],
        reference = row["Reference"]?.ToString(),
        status = row["Status"]?.ToString(),
        totalHours = (decimal)row["TotalHours"],
        weekStartDate = ((DateTime)row["WeekStartDate"]).ToString("yyyy-MM-dd"),
        submittedDate = row["SubmittedDate"] != DBNull.Value ? ((DateTime)row["SubmittedDate"]).ToString("yyyy-MM-dd") : null,
        approvedDate = row["ApprovedDate"] != DBNull.Value ? ((DateTime)row["ApprovedDate"]).ToString("yyyy-MM-dd") : null,
        notes = row["Notes"]?.ToString(),
        entries = lineItems
    });
}).RequireAuthorization();

app.MapPost("/api/mobile/timesheets", async (HttpContext ctx, DatabaseService db) =>
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

    var reference = $"TS-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var weekStartDate = req.weekStartDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

    await db.ExecuteNonQueryAsync(
        "INSERT INTO Timesheets (Reference, EmployeeId, TenantId, WeekStartDate, [Status], TotalHours) VALUES (@Ref, @EmpId, @TenantId, @WeekStart, 'Draft', 0)",
        new() { ["Ref"] = reference, ["EmpId"] = int.Parse(userId), ["TenantId"] = Guid.Parse(tenantId), ["WeekStart"] = weekStartDate });

    return Results.Ok(new { reference, status = "Draft" });
}).RequireAuthorization();

// ── Mobile Tasks ─────────────────────────────────────────────────────────────
app.MapGet("/api/mobile/tasks", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 50 TaskId, Reference, Title, [Status], Priority, DueDate FROM Tasks WHERE TenantId = @TenantId ORDER BY DueDate DESC",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var tasks = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        tasks.Add(new
        {
            id = (int)row["TaskId"],
            reference = row["Reference"]?.ToString(),
            title = row["Title"]?.ToString(),
            status = row["Status"]?.ToString(),
            priority = row["Priority"]?.ToString(),
            dueDate = row["DueDate"] != DBNull.Value ? ((DateTime)row["DueDate"]).ToString("yyyy-MM-dd") : null
        });
    }
    return Results.Ok(tasks);
}).RequireAuthorization();

app.MapGet("/api/mobile/tasks/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TaskId, Reference, Title, [Description], [Status], Priority, DueDate, ProjectName, CreatedAt, CompletedAt FROM Tasks WHERE TaskId = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound();

    var row = dt.Rows[0];
    var comments = await db.QueryAsync(
        "SELECT CommentId, CommentText, CreatedAt FROM TaskComments WHERE TaskId = @TaskId ORDER BY CreatedAt DESC",
        new() { ["TaskId"] = id });

    var commentsList = new List<object>();
    foreach (DataRow comment in comments.Rows)
    {
        commentsList.Add(new
        {
            id = (int)comment["CommentId"],
            text = comment["CommentText"]?.ToString(),
            createdAt = ((DateTime)comment["CreatedAt"]).ToString("yyyy-MM-dd HH:mm")
        });
    }

    return Results.Ok(new
    {
        id = (int)row["TaskId"],
        reference = row["Reference"]?.ToString(),
        title = row["Title"]?.ToString(),
        description = row["Description"]?.ToString(),
        status = row["Status"]?.ToString(),
        priority = row["Priority"]?.ToString(),
        dueDate = row["DueDate"] != DBNull.Value ? ((DateTime)row["DueDate"]).ToString("yyyy-MM-dd") : null,
        projectName = row["ProjectName"]?.ToString(),
        createdAt = ((DateTime)row["CreatedAt"]).ToString("yyyy-MM-dd"),
        completedAt = row["CompletedAt"] != DBNull.Value ? ((DateTime)row["CompletedAt"]).ToString("yyyy-MM-dd") : null,
        comments = commentsList
    });
}).RequireAuthorization();

app.MapPost("/api/mobile/tasks", async (HttpContext ctx, DatabaseService db) =>
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

    var reference = $"TSK-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var title = req.title ?? "New Task";
    var description = req.description ?? "";

    await db.ExecuteNonQueryAsync(
        "INSERT INTO Tasks (Reference, Title, [Description], TenantId, CreatedById, [Status], Priority) VALUES (@Ref, @Title, @Desc, @TenantId, @CreatedById, 'ToDo', 'Normal')",
        new() { ["Ref"] = reference, ["Title"] = title, ["Desc"] = description, ["TenantId"] = Guid.Parse(tenantId), ["CreatedById"] = int.Parse(userId) });

    return Results.Ok(new { reference, status = "ToDo" });
}).RequireAuthorization();

app.MapPut("/api/mobile/tasks/{id:int}/status", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    dynamic? req;
    try { req = await ctx.Request.ReadFromJsonAsync<dynamic>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }

    if (req is null || string.IsNullOrWhiteSpace(req.status))
        return Results.BadRequest(new { error = "status required" });

    var status = req.status;
    var completedAt = status == "Done" ? DateTime.UtcNow : (DateTime?)null;

    await db.ExecuteNonQueryAsync(
        "UPDATE Tasks SET [Status] = @Status, CompletedAt = @CompletedAt, UpdatedAt = GETUTCDATE() WHERE TaskId = @Id AND TenantId = @TenantId",
        new() { ["Status"] = status, ["CompletedAt"] = (object?)completedAt ?? DBNull.Value, ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    return Results.Ok(new { status });
}).RequireAuthorization();

// ── Mobile Despatch ──────────────────────────────────────────────────────────
app.MapGet("/api/mobile/despatch", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 50 DespatchId, Reference, [Status], DeliveryDate, RecipientName FROM Despatch WHERE TenantId = @TenantId ORDER BY DeliveryDate DESC",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var despatch = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        despatch.Add(new
        {
            id = (int)row["DespatchId"],
            reference = row["Reference"]?.ToString(),
            status = row["Status"]?.ToString(),
            deliveryDate = ((DateTime)row["DeliveryDate"]).ToString("yyyy-MM-dd"),
            recipientName = row["RecipientName"]?.ToString()
        });
    }
    return Results.Ok(despatch);
}).RequireAuthorization();

app.MapGet("/api/mobile/despatch/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT DespatchId, Reference, [Status], DeliveryDate, RecipientName, RecipientAddress, RecipientPhone, DeliveredDate, Notes FROM Despatch WHERE DespatchId = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound();

    var row = dt.Rows[0];
    var items = await db.QueryAsync(
        "SELECT ItemId, Description, Quantity, Unit FROM DespatchItems WHERE DespatchId = @DespatchId ORDER BY LineNumber",
        new() { ["DespatchId"] = id });

    var lineItems = new List<object>();
    foreach (DataRow item in items.Rows)
    {
        lineItems.Add(new
        {
            id = (int)item["ItemId"],
            description = item["Description"]?.ToString(),
            quantity = (int)item["Quantity"],
            unit = item["Unit"]?.ToString()
        });
    }

    return Results.Ok(new
    {
        id = (int)row["DespatchId"],
        reference = row["Reference"]?.ToString(),
        status = row["Status"]?.ToString(),
        deliveryDate = ((DateTime)row["DeliveryDate"]).ToString("yyyy-MM-dd"),
        recipientName = row["RecipientName"]?.ToString(),
        recipientAddress = row["RecipientAddress"]?.ToString(),
        recipientPhone = row["RecipientPhone"]?.ToString(),
        deliveredDate = row["DeliveredDate"] != DBNull.Value ? ((DateTime)row["DeliveredDate"]).ToString("yyyy-MM-dd") : null,
        notes = row["Notes"]?.ToString(),
        items = lineItems
    });
}).RequireAuthorization();

app.MapPost("/api/mobile/despatch", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    dynamic? req;
    try { req = await ctx.Request.ReadFromJsonAsync<dynamic>(); }
    catch { return Results.BadRequest(new { error = "Invalid JSON" }); }

    if (req is null) return Results.BadRequest(new { error = "Request body required" });

    var reference = $"DSP-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    var deliveryDate = req.deliveryDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
    var recipientName = req.recipientName ?? "";

    await db.ExecuteNonQueryAsync(
        "INSERT INTO Despatch (Reference, TenantId, DeliveryDate, [Status], RecipientName) VALUES (@Ref, @TenantId, @DeliveryDate, 'Pending', @RecipientName)",
        new() { ["Ref"] = reference, ["TenantId"] = Guid.Parse(tenantId), ["DeliveryDate"] = deliveryDate, ["RecipientName"] = recipientName });

    return Results.Ok(new { reference, status = "Pending" });
}).RequireAuthorization();

app.MapPut("/api/mobile/despatch/{id:int}/deliver", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("sub")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        return Results.BadRequest(new { error = "Missing context" });

    await db.ExecuteNonQueryAsync(
        "UPDATE Despatch SET [Status] = 'Delivered', DeliveredDate = GETUTCDATE(), DeliveredBy = @UserId, UpdatedAt = GETUTCDATE() WHERE DespatchId = @Id AND TenantId = @TenantId",
        new() { ["UserId"] = int.Parse(userId), ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    return Results.Ok(new { status = "Delivered" });
}).RequireAuthorization();

// ── Mobile Contacts ──────────────────────────────────────────────────────────
app.MapGet("/api/mobile/contacts", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 50 ContactId, Reference, FirstName, LastName, Email, Phone, [Role] FROM Contacts WHERE TenantId = @TenantId ORDER BY LastName, FirstName",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var contacts = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        contacts.Add(new
        {
            id = (int)row["ContactId"],
            reference = row["Reference"]?.ToString(),
            firstName = row["FirstName"]?.ToString(),
            lastName = row["LastName"]?.ToString(),
            email = row["Email"]?.ToString(),
            phone = row["Phone"]?.ToString(),
            role = row["Role"]?.ToString()
        });
    }
    return Results.Ok(contacts);
}).RequireAuthorization();

app.MapGet("/api/mobile/contacts/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT ContactId, Reference, FirstName, LastName, Email, Phone, Mobile, [Address], [Role] FROM Contacts WHERE ContactId = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound();

    var row = dt.Rows[0];
    return Results.Ok(new
    {
        id = (int)row["ContactId"],
        reference = row["Reference"]?.ToString(),
        firstName = row["FirstName"]?.ToString(),
        lastName = row["LastName"]?.ToString(),
        email = row["Email"]?.ToString(),
        phone = row["Phone"]?.ToString(),
        mobile = row["Mobile"]?.ToString(),
        address = row["Address"]?.ToString(),
        role = row["Role"]?.ToString()
    });
}).RequireAuthorization();

// ── Mobile Cash Flow ─────────────────────────────────────────────────────────
app.MapGet("/api/mobile/cashflow", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 12 ForecastDate, ProjectedIncoming, ProjectedOutgoing, CashPosition FROM CashFlowForecasts WHERE TenantId = @TenantId ORDER BY ForecastDate ASC",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var forecast = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        forecast.Add(new
        {
            date = ((DateTime)row["ForecastDate"]).ToString("yyyy-MM-dd"),
            projectedIncoming = row["ProjectedIncoming"] != DBNull.Value ? (decimal)row["ProjectedIncoming"] : 0,
            projectedOutgoing = row["ProjectedOutgoing"] != DBNull.Value ? (decimal)row["ProjectedOutgoing"] : 0,
            cashPosition = row["CashPosition"] != DBNull.Value ? (decimal?)row["CashPosition"] : null
        });
    }
    return Results.Ok(new { forecast, weeks = forecast.Count });
}).RequireAuthorization();

// ── Mobile Goals (KPIs) ──────────────────────────────────────────────────────
app.MapGet("/api/mobile/goals", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT GoalId, Reference, Title, TargetValue, CurrentValue, UnitOfMeasure, [Period], [Status] FROM BusinessGoals WHERE TenantId = @TenantId AND [Status] = 'Active' ORDER BY [Period]",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var goals = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        var target = row["TargetValue"] != DBNull.Value ? (decimal)row["TargetValue"] : 0;
        var current = row["CurrentValue"] != DBNull.Value ? (decimal)row["CurrentValue"] : 0;
        var percentage = target > 0 ? (int)((current / target) * 100) : 0;

        goals.Add(new
        {
            id = (int)row["GoalId"],
            reference = row["Reference"]?.ToString(),
            title = row["Title"]?.ToString(),
            targetValue = target,
            currentValue = current,
            unitOfMeasure = row["UnitOfMeasure"]?.ToString(),
            period = row["Period"]?.ToString(),
            progressPercentage = percentage
        });
    }
    return Results.Ok(new { goals, count = goals.Count });
}).RequireAuthorization();

// ── Mobile Projects ──────────────────────────────────────────────────────────
app.MapGet("/api/mobile/projects", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT TOP 50 ProjectId, ProjectCode, ProjectName, [Status], [Percent], Health FROM Projects WHERE TenantId = @TenantId ORDER BY ProjectName",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var projects = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        projects.Add(new
        {
            id = (int)row["ProjectId"],
            code = row["ProjectCode"]?.ToString(),
            name = row["ProjectName"]?.ToString(),
            status = row["Status"]?.ToString(),
            progressPercentage = (int)row["Percent"],
            health = row["Health"]?.ToString()
        });
    }
    return Results.Ok(projects);
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

// ─────────────────────────────────────────────────────────────────────────────
// APPROVAL WORKFLOWS API
// ─────────────────────────────────────────────────────────────────────────────
// TEMPORARILY DISABLED FOR DIAGNOSTICS
/*
// ── Get Approval Workflows ──────────────────────────────────────────────────
app.MapGet("/api/approval/workflows", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var dt = await db.QueryAsync(
        "SELECT WorkflowId, TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels, CreatedAt FROM ApprovalWorkflows WHERE TenantId = @TenantId ORDER BY ModuleType, [Name]",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    var workflows = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        workflows.Add(new
        {
            id = (int)row["WorkflowId"],
            moduleType = (string)row["ModuleType"],
            name = (string)row["Name"],
            description = row["Description"] != DBNull.Value ? (string)row["Description"] : null,
            isDefault = (bool)row["IsDefault"],
            approvalLevels = (int)row["ApprovalLevels"],
            createdAt = ((DateTime)row["CreatedAt"]).ToString("yyyy-MM-dd")
        });
    }
    return Results.Ok(new { workflows, totalCount = workflows.Count });
}).RequireAuthorization();

// ── Submit Expense for Approval ────────────────────────────────────────────
app.MapPost("/api/expenses/{id}/submit-for-approval", async (int id, HttpContext ctx, DatabaseService db, NotificationService notificationSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var expenseDt = await db.QueryAsync(
        "SELECT ExpenseId, Status, Total, Description FROM Expenses WHERE ExpenseId = @ExpenseId AND TenantId = @TenantId",
        new() { ["ExpenseId"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (expenseDt.Rows.Count == 0) return Results.NotFound(new { error = "Expense not found" });

    decimal expenseAmount = (decimal?)expenseDt.Rows[0]["Total"] ?? 0;
    string expenseDesc = expenseDt.Rows[0]["Description"]?.ToString() ?? "Expense";

    var workflowDt = await db.QueryAsync(
        "SELECT WorkflowId FROM ApprovalWorkflows WHERE TenantId = @TenantId AND ModuleType = 'Expense' AND IsDefault = 1",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    if (workflowDt.Rows.Count == 0) return Results.BadRequest(new { error = "No default approval workflow configured" });

    int workflowId = (int)workflowDt.Rows[0]["WorkflowId"];

    await db.ExecuteNonQueryAsync(
        "INSERT INTO ApprovalRequests (TenantId, WorkflowId, ModuleType, ModuleId, SubmittedById, SubmittedAt, [Status], CurrentLevel) VALUES (@TenantId, @WorkflowId, 'Expense', @ModuleId, @UserId, GETUTCDATE(), 'Pending', 1)",
        new() { ["TenantId"] = Guid.Parse(tenantId), ["WorkflowId"] = workflowId, ["ModuleId"] = id, ["UserId"] = userId });

    // Get submitter name
    var submitterDt = await db.QueryAsync(
        "SELECT Name FROM Users WHERE UserId = @UserId",
        new() { ["UserId"] = userId });
    string submitterName = submitterDt.Rows.Count > 0 ? (string)submitterDt.Rows[0]["Name"] : "Employee";

    // Send notification to approvers with approval permissions
    int tenantIdInt = int.TryParse(tenantId, out int t) ? t : 0;
    if (tenantIdInt > 0)
    {
        var approversDt = await db.QueryAsync(
            @"SELECT DISTINCT UserId FROM dbo.ApprovalPermissions
              WHERE TenantId = @TenantId AND ModuleType = 'Expense' AND IsActive = 1
              AND ApprovalLevel = 1",
            new() { ["TenantId"] = tenantIdInt });

        var approverIds = new List<int>();
        foreach (var row in approversDt.Rows)
        {
            if (row["UserId"] != DBNull.Value && int.TryParse(row["UserId"].ToString(), out int approverId))
                approverIds.Add(approverId);
        }

        if (approverIds.Count > 0)
        {
            await notificationSvc.SendBulkNotificationAsync(
                tenantIdInt,
                approverIds,
                "ExpenseSubmittedForApproval",
                new Dictionary<string, object>
                {
                    { "SubmitterName", submitterName },
                    { "CurrencySymbol", "$" },
                    { "Amount", expenseAmount.ToString("F2") },
                    { "Description", expenseDesc }
                },
                "Expense",
                id,
                userId);
        }
    }

    return Results.Ok(new { message = "Expense submitted for approval", requestId = id });
}).RequireAuthorization();

// ── Submit Timesheet for Approval ──────────────────────────────────────────
app.MapPost("/api/timesheets/{id}/submit-for-approval", async (int id, HttpContext ctx, DatabaseService db, NotificationService notificationSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var timesheetDt = await db.QueryAsync(
        "SELECT TimesheetId, [Status], TotalHours FROM Timesheets WHERE TimesheetId = @TimesheetId AND TenantId = @TenantId",
        new() { ["TimesheetId"] = id, ["TenantId"] = Guid.Parse(tenantId) });

    if (timesheetDt.Rows.Count == 0) return Results.NotFound(new { error = "Timesheet not found" });

    decimal totalHours = (decimal?)timesheetDt.Rows[0]["TotalHours"] ?? 0;

    var workflowDt = await db.QueryAsync(
        "SELECT WorkflowId FROM ApprovalWorkflows WHERE TenantId = @TenantId AND ModuleType = 'Timesheet' AND IsDefault = 1",
        new() { ["TenantId"] = Guid.Parse(tenantId) });

    if (workflowDt.Rows.Count == 0) return Results.BadRequest(new { error = "No default approval workflow configured" });

    int workflowId = (int)workflowDt.Rows[0]["WorkflowId"];

    await db.ExecuteNonQueryAsync(
        "INSERT INTO ApprovalRequests (TenantId, WorkflowId, ModuleType, ModuleId, SubmittedById, SubmittedAt, [Status], CurrentLevel) VALUES (@TenantId, @WorkflowId, 'Timesheet', @ModuleId, @UserId, GETUTCDATE(), 'Pending', 1)",
        new() { ["TenantId"] = Guid.Parse(tenantId), ["WorkflowId"] = workflowId, ["ModuleId"] = id, ["UserId"] = userId });

    // Get submitter name
    var submitterDt = await db.QueryAsync(
        "SELECT Name FROM Users WHERE UserId = @UserId",
        new() { ["UserId"] = userId });
    string submitterName = submitterDt.Rows.Count > 0 ? (string)submitterDt.Rows[0]["Name"] : "Employee";

    // Send notification to approvers with approval permissions
    int tenantIdInt = int.TryParse(tenantId, out int t) ? t : 0;
    if (tenantIdInt > 0)
    {
        var approversDt = await db.QueryAsync(
            @"SELECT DISTINCT UserId FROM dbo.ApprovalPermissions
              WHERE TenantId = @TenantId AND ModuleType = 'Timesheet' AND IsActive = 1
              AND ApprovalLevel = 1",
            new() { ["TenantId"] = tenantIdInt });

        var approverIds = new List<int>();
        foreach (var row in approversDt.Rows)
        {
            if (row["UserId"] != DBNull.Value && int.TryParse(row["UserId"].ToString(), out int approverId))
                approverIds.Add(approverId);
        }

        if (approverIds.Count > 0)
        {
            await notificationSvc.SendBulkNotificationAsync(
                tenantIdInt,
                approverIds,
                "TimesheetSubmittedForApproval",
                new Dictionary<string, object>
                {
                    { "SubmitterName", submitterName },
                    { "Hours", totalHours.ToString("F1") }
                },
                "Timesheet",
                id,
                userId);
        }
    }

    return Results.Ok(new { message = "Timesheet submitted for approval", requestId = id });
}).RequireAuthorization();

// ── Get Pending Approvals ───────────────────────────────────────────────────
app.MapGet("/api/approval/pending", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var dt = await db.QueryAsync(
        @"SELECT ar.RequestId, ar.ModuleType, ar.ModuleId, ar.CurrentLevel, ar.SubmittedAt,
                 u.[Name] AS SubmitterName, ar.[Status]
          FROM ApprovalRequests ar
          JOIN ApprovalRules ar2 ON ar2.WorkflowId = ar.WorkflowId AND ar2.[Level] = ar.CurrentLevel
          JOIN Users u ON u.UserId = ar.SubmittedById
          WHERE ar.TenantId = @TenantId AND ar.[Status] = 'Pending'
            AND (ar2.ApproverUserId = @UserId OR ar2.ApproverRole IS NOT NULL)
          ORDER BY ar.SubmittedAt",
        new() { ["TenantId"] = Guid.Parse(tenantId), ["UserId"] = userId });

    var approvals = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        approvals.Add(new
        {
            requestId = (int)row["RequestId"],
            moduleType = (string)row["ModuleType"],
            moduleId = (int)row["ModuleId"],
            currentLevel = (int)row["CurrentLevel"],
            submitterName = (string)row["SubmitterName"],
            submittedAt = ((DateTime)row["SubmittedAt"]).ToString("yyyy-MM-dd HH:mm")
        });
    }
    return Results.Ok(new { approvals, count = approvals.Count });
}).RequireAuthorization();

// ── Approve Request ─────────────────────────────────────────────────────────
app.MapPost("/api/approval/requests/{requestId}/approve", async (int requestId, HttpContext ctx, DatabaseService db, NotificationService notificationSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var requestDt = await db.QueryAsync(
        "SELECT RequestId, [Status], CurrentLevel, WorkflowId, SubmittedByUserId, Amount FROM ApprovalRequests WHERE RequestId = @RequestId AND TenantId = @TenantId",
        new() { ["RequestId"] = requestId, ["TenantId"] = Guid.Parse(tenantId) });

    if (requestDt.Rows.Count == 0) return Results.NotFound(new { error = "Request not found" });

    if (requestDt.Rows[0]["Status"].ToString() != "Pending")
        return Results.BadRequest(new { error = "Request is not pending" });

    int currentLevel = (int)requestDt.Rows[0]["CurrentLevel"];
    int workflowId = (int)requestDt.Rows[0]["WorkflowId"];
    int submittedByUserId = (int?)requestDt.Rows[0]["SubmittedByUserId"] ?? 0;
    decimal amount = (decimal?)requestDt.Rows[0]["Amount"] ?? 0;

    var workflowDt = await db.QueryAsync(
        "SELECT ApprovalLevels FROM ApprovalWorkflows WHERE WorkflowId = @WorkflowId",
        new() { ["WorkflowId"] = workflowId });

    int totalLevels = (int)workflowDt.Rows[0]["ApprovalLevels"];
    bool isFinal = currentLevel >= totalLevels;

    // Phase 1 Week 2: Check approval permissions before allowing approval
    var userRole = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
    var expenseCheckDt = await db.QueryAsync(
        @"SELECT ModuleType, (CASE WHEN ModuleType = 'Expense' THEN Amount ELSE 0 END) AS Amount
          FROM ApprovalRequests
          WHERE RequestId = @RequestId",
        new() { ["RequestId"] = requestId });

    if (expenseCheckDt.Rows.Count > 0)
    {
        string moduleType = expenseCheckDt.Rows[0]["ModuleType"]?.ToString() ?? "Expense";
        decimal chkAmount = (decimal)(expenseCheckDt.Rows[0]["Amount"] ?? 0);

        var permDt = await db.QueryAsync(
            @"SELECT COUNT(*) as cnt FROM dbo.ApprovalPermissions
              WHERE TenantId = @TenantId
                AND ModuleType = @ModuleType
                AND ApprovalLevel = @Level
                AND IsActive = 1
                AND (
                  (UserId = @UserId AND CanReject = 1)
                  OR (RoleId = @Role AND CanReject = 1)
                )
                AND (MinThreshold IS NULL OR MinThreshold <= @Amount)
                AND (MaxThreshold IS NULL OR MaxThreshold >= @Amount)",
            new()
            {
                ["TenantId"] = Guid.Parse(tenantId),
                ["ModuleType"] = moduleType,
                ["Level"] = currentLevel,
                ["UserId"] = userId,
                ["Role"] = userRole ?? "none",
                ["Amount"] = chkAmount
            });

        int permCount = (int)permDt.Rows[0]["cnt"];
        if (permCount == 0)
        {
            Log.Warning("Approval permission denied for UserId={UserId} on RequestId={RequestId} Level={Level}",
                userId, requestId, currentLevel);
            return Results.Forbid();
        }
    }

    // Get approver name for notification
    var approverDt = await db.QueryAsync(
        "SELECT Name FROM Users WHERE UserId = @UserId",
        new() { ["UserId"] = userId });
    string approverName = approverDt.Rows.Count > 0 ? (string)approverDt.Rows[0]["Name"] : "Manager";

    await db.ExecuteNonQueryAsync(
        "INSERT INTO ApprovalActions (RequestId, ApprovalLevel, ApprovedById, [Action], ActionAt) VALUES (@RequestId, @Level, @UserId, 'Approved', GETUTCDATE())",
        new() { ["RequestId"] = requestId, ["Level"] = currentLevel, ["UserId"] = userId });

    if (isFinal)
    {
        await db.ExecuteNonQueryAsync(
            "UPDATE ApprovalRequests SET [Status] = 'Approved', CompletedAt = GETUTCDATE() WHERE RequestId = @RequestId",
            new() { ["RequestId"] = requestId });

        // Send notification to submitter
        if (submittedByUserId > 0)
        {
            int tenantIdInt = int.TryParse(tenantId, out int t) ? t : 0;
            if (tenantIdInt > 0)
            {
                await notificationSvc.SendNotificationAsync(
                    tenantIdInt,
                    submittedByUserId,
                    "ApprovalApproved",
                    new Dictionary<string, object>
                    {
                        { "ApproverName", approverName },
                        { "CurrencySymbol", "$" },
                        { "Amount", amount.ToString("F2") }
                    },
                    "ApprovalRequest",
                    requestId,
                    userId);
            }
        }

        return Results.Ok(new { message = "Request approved", finalApproval = true });
    }
    else
    {
        await db.ExecuteNonQueryAsync(
            "UPDATE ApprovalRequests SET CurrentLevel = @NextLevel WHERE RequestId = @RequestId",
            new() { ["NextLevel"] = currentLevel + 1, ["RequestId"] = requestId });
        return Results.Ok(new { message = "Approved, forwarding to next level", nextLevel = currentLevel + 1 });
    }
}).RequireAuthorization();

// ── Reject Request ──────────────────────────────────────────────────────────
app.MapPost("/api/approval/requests/{requestId}/reject", async (int requestId, HttpContext ctx, DatabaseService db, NotificationService notificationSvc) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var requestDt = await db.QueryAsync(
        "SELECT RequestId, [Status], SubmittedByUserId, Amount FROM ApprovalRequests WHERE RequestId = @RequestId AND TenantId = @TenantId",
        new() { ["RequestId"] = requestId, ["TenantId"] = Guid.Parse(tenantId) });

    if (requestDt.Rows.Count == 0) return Results.NotFound(new { error = "Request not found" });
    if (requestDt.Rows[0]["Status"].ToString() != "Pending")
        return Results.BadRequest(new { error = "Request is not pending" });

    int submittedByUserId = (int?)requestDt.Rows[0]["SubmittedByUserId"] ?? 0;
    decimal amount = (decimal?)requestDt.Rows[0]["Amount"] ?? 0;

    var currentDt = await db.QueryAsync(
        "SELECT CurrentLevel FROM ApprovalRequests WHERE RequestId = @RequestId",
        new() { ["RequestId"] = requestId });

    int currentLevel = (int)currentDt.Rows[0]["CurrentLevel"];

    // Get approver name for notification
    var approverDt = await db.QueryAsync(
        "SELECT Name FROM Users WHERE UserId = @UserId",
        new() { ["UserId"] = userId });
    string approverName = approverDt.Rows.Count > 0 ? (string)approverDt.Rows[0]["Name"] : "Manager";

    await db.ExecuteNonQueryAsync(
        "INSERT INTO ApprovalActions (RequestId, ApprovalLevel, ApprovedById, [Action], ActionAt) VALUES (@RequestId, @Level, @UserId, 'Rejected', GETUTCDATE())",
        new() { ["RequestId"] = requestId, ["Level"] = currentLevel, ["UserId"] = userId });

    await db.ExecuteNonQueryAsync(
        "UPDATE ApprovalRequests SET [Status] = 'Rejected', CompletedAt = GETUTCDATE() WHERE RequestId = @RequestId",
        new() { ["RequestId"] = requestId });

    // Send notification to submitter
    if (submittedByUserId > 0)
    {
        int tenantIdInt = int.TryParse(tenantId, out int t) ? t : 0;
        if (tenantIdInt > 0)
        {
            await notificationSvc.SendNotificationAsync(
                tenantIdInt,
                submittedByUserId,
                "ApprovalRejected",
                new Dictionary<string, object>
                {
                    { "ApproverName", approverName },
                    { "CurrencySymbol", "$" },
                    { "Amount", amount.ToString("F2") },
                    { "RejectionReason", "Please contact your manager for details" }
                },
                "ApprovalRequest",
                requestId,
                userId);
        }
    }

    return Results.Ok(new { message = "Request rejected" });
}).RequireAuthorization();

// ── Get Approval History ────────────────────────────────────────────────────
app.MapGet("/api/approval/requests/{requestId}/history", async (int requestId, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrWhiteSpace(tenantId)) return Results.BadRequest(new { error = "No tenant context" });

    var requestDt = await db.QueryAsync(
        "SELECT RequestId FROM ApprovalRequests WHERE RequestId = @RequestId AND TenantId = @TenantId",
        new() { ["RequestId"] = requestId, ["TenantId"] = Guid.Parse(tenantId) });

    if (requestDt.Rows.Count == 0) return Results.NotFound(new { error = "Request not found" });

    var dt = await db.QueryAsync(
        @"SELECT aa.ActionId, aa.ApprovalLevel, aa.[Action], aa.[Comments], u.[Name] AS ApprovedBy, aa.ActionAt
          FROM ApprovalActions aa
          LEFT JOIN Users u ON u.UserId = aa.ApprovedById
          WHERE aa.RequestId = @RequestId
          ORDER BY aa.ActionAt",
        new() { ["RequestId"] = requestId });

    var actions = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        actions.Add(new
        {
            level = (int)row["ApprovalLevel"],
            action = (string)row["Action"],
            comments = row["Comments"] != DBNull.Value ? (string)row["Comments"] : null,
            approvedBy = row["ApprovedBy"] != DBNull.Value ? (string)row["ApprovedBy"] : "System",
            actionAt = ((DateTime)row["ActionAt"]).ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
    return Results.Ok(new { actions, timeline = actions.Count });
}).RequireAuthorization();

// ─────────────────────────────────────────────────────────────────────────────
// APPROVAL DELEGATIONS API
// ─────────────────────────────────────────────────────────────────────────────

// ── Create Delegation ───────────────────────────────────────────────────────
app.MapPost("/api/approval/delegations", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var body = await ctx.Request.ReadFromJsonAsync<DelegationRequest>();
    if (body?.DelegateUserId == 0 || body?.StartDate == null || body?.EndDate == null)
        return Results.BadRequest(new { error = "delegateUserId, startDate, and endDate are required" });

    if (body.EndDate < body.StartDate)
        return Results.BadRequest(new { error = "endDate must be after startDate" });

    await db.ExecuteNonQueryAsync(
        @"INSERT INTO ApprovalDelegations (TenantId, ApproverUserId, DelegateUserId, StartDate, EndDate, ModuleType, IsActive)
          VALUES (@TenantId, @ApproverUserId, @DelegateUserId, @StartDate, @EndDate, @ModuleType, 1)",
        new()
        {
            ["TenantId"] = Guid.Parse(tenantId),
            ["ApproverUserId"] = userId,
            ["DelegateUserId"] = body.DelegateUserId,
            ["StartDate"] = body.StartDate,
            ["EndDate"] = body.EndDate,
            ["ModuleType"] = body.ModuleType ?? (object)DBNull.Value
        });

    return Results.Ok(new { message = "Delegation created successfully", delegationId = userId });
}).RequireAuthorization();

// ── Get Active Delegations ──────────────────────────────────────────────────
app.MapGet("/api/approval/delegations", async (HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var dt = await db.QueryAsync(
        @"SELECT DelegationId, ApproverUserId, DelegateUserId, StartDate, EndDate, ModuleType, IsActive
          FROM ApprovalDelegations
          WHERE TenantId = @TenantId AND ApproverUserId = @ApproverUserId AND IsActive = 1
          AND EndDate >= CAST(GETUTCDATE() AS DATE)
          ORDER BY StartDate DESC",
        new() { ["TenantId"] = Guid.Parse(tenantId), ["ApproverUserId"] = userId });

    var delegations = new List<object>();
    foreach (DataRow row in dt.Rows)
    {
        delegations.Add(new
        {
            delegationId = (int)row["DelegationId"],
            delegateUserId = (int)row["DelegateUserId"],
            startDate = ((DateTime)row["StartDate"]).ToString("yyyy-MM-dd"),
            endDate = ((DateTime)row["EndDate"]).ToString("yyyy-MM-dd"),
            moduleType = row["ModuleType"] != DBNull.Value ? (string)row["ModuleType"] : null,
            isActive = (bool)row["IsActive"]
        });
    }
    return Results.Ok(new { delegations, totalCount = delegations.Count });
}).RequireAuthorization();

// ── Revoke Delegation ───────────────────────────────────────────────────────
app.MapDelete("/api/approval/delegations/{delegationId:int}", async (int delegationId, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var dt = await db.QueryAsync(
        @"SELECT DelegationId, ApproverUserId FROM ApprovalDelegations
          WHERE DelegationId = @DelegationId AND TenantId = @TenantId",
        new() { ["DelegationId"] = delegationId, ["TenantId"] = Guid.Parse(tenantId) });

    if (dt.Rows.Count == 0) return Results.NotFound(new { error = "Delegation not found" });
    if ((int)dt.Rows[0]["ApproverUserId"] != userId)
        return Results.Forbid();

    await db.ExecuteNonQueryAsync(
        "UPDATE ApprovalDelegations SET IsActive = 0 WHERE DelegationId = @DelegationId",
        new() { ["DelegationId"] = delegationId });

    return Results.Ok(new { message = "Delegation revoked" });
}).RequireAuthorization();

// ── Delegate Approval Request ───────────────────────────────────────────────
app.MapPost("/api/approval/requests/{requestId}/delegate", async (int requestId, HttpContext ctx, DatabaseService db) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = int.TryParse(ctx.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
    if (string.IsNullOrWhiteSpace(tenantId) || userId == 0) return Results.BadRequest(new { error = "Invalid context" });

    var body = await ctx.Request.ReadFromJsonAsync<DelegateApprovalRequest>();
    if (body?.DelegateUserId == 0) return Results.BadRequest(new { error = "delegateUserId is required" });

    var requestDt = await db.QueryAsync(
        "SELECT RequestId, [Status] FROM ApprovalRequests WHERE RequestId = @RequestId AND TenantId = @TenantId",
        new() { ["RequestId"] = requestId, ["TenantId"] = Guid.Parse(tenantId) });

    if (requestDt.Rows.Count == 0) return Results.NotFound(new { error = "Request not found" });
    if (requestDt.Rows[0]["Status"].ToString() != "Pending")
        return Results.BadRequest(new { error = "Only pending requests can be delegated" });

    var currentDt = await db.QueryAsync(
        "SELECT CurrentLevel FROM ApprovalRequests WHERE RequestId = @RequestId",
        new() { ["RequestId"] = requestId });

    int currentLevel = (int)currentDt.Rows[0]["CurrentLevel"];

    await db.ExecuteNonQueryAsync(
        @"INSERT INTO ApprovalActions (RequestId, ApprovalLevel, ApprovedById, [Action], DelegatedToUserId, ActionAt)
          VALUES (@RequestId, @Level, @UserId, 'Delegated', @DelegateUserId, GETUTCDATE())",
        new() { ["RequestId"] = requestId, ["Level"] = currentLevel, ["UserId"] = userId, ["DelegateUserId"] = body.DelegateUserId });

    return Results.Ok(new { message = "Request delegated successfully", delegatedTo = body.DelegateUserId });
}).RequireAuthorization();

// ── Domain-Based Tenant Routing (Phase 1 Week 1) ──────────────────────────────────────
// Critical for domain-based multi-tenancy and Australian Privacy Act compliance

// GET /api/tenant/resolve-domain - Resolve email domain to tenant
app.MapGet("/api/tenant/resolve-domain", async (string emailDomain, DatabaseService db) =>
{
    if (string.IsNullOrWhiteSpace(emailDomain))
        return Results.BadRequest(new { error = "Email domain is required" });

    // Remove @ symbol if present
    emailDomain = emailDomain.TrimStart('@').ToLower();

    var result = await db.QueryAsync(
        @"SELECT TenantId, Domain, IsVerified, IsActive
          FROM dbo.TenantDomains
          WHERE Domain = @Domain AND IsActive = 1 AND IsVerified = 1",
        new() { ["Domain"] = emailDomain });

    if (result.Rows.Count == 0)
        return Results.NotFound(new { error = $"No verified tenant found for domain '{emailDomain}'" });

    int tenantId = (int)result.Rows[0]["TenantId"];
    string domain = (string)result.Rows[0]["Domain"];
    bool isVerified = (bool)result.Rows[0]["IsVerified"];

    return Results.Ok(new
    {
        tenantId,
        domain,
        isVerified,
        message = "Domain resolved successfully"
    });
})
.WithName("ResolveDomain")
// .WithOpenApi()  // DISABLED - method not available
.AllowAnonymous();  // Allow before authentication for login flow

// GET /api/tenant/verify-domain-status - Check verification status
app.MapGet("/api/tenant/verify-domain-status", async (string domain, DatabaseService db) =>
{
    domain = domain.TrimStart('@').ToLower();

    var result = await db.QueryAsync(
        @"SELECT TenantId, IsVerified, VerificationToken, VerificationTokenExpiry
          FROM dbo.TenantDomains
          WHERE Domain = @Domain AND IsActive = 1",
        new() { ["Domain"] = domain });

    if (result.Rows.Count == 0)
        return Results.NotFound(new { error = "Domain not found" });

    bool isVerified = (bool)result.Rows[0]["IsVerified"];
    string verificationToken = result.Rows[0]["VerificationToken"]?.ToString() ?? "";
    object verificationExpiry = result.Rows[0]["VerificationTokenExpiry"] ?? null;

    return Results.Ok(new
    {
        domain,
        isVerified,
        verificationToken = isVerified ? null : verificationToken,
        verificationTokenExpiry = isVerified ? null : verificationExpiry,
        status = isVerified ? "verified" : "pending_verification"
    });
})
.WithName("VerifyDomainStatus")
// .WithOpenApi()  // DISABLED - method not available
.AllowAnonymous();

// POST /api/tenant/add-domain - Admin: Add new domain for their tenant (requires auth)
app.MapPost("/api/tenant/add-domain", async (HttpContext ctx, AddDomainRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(body.Domain))
        return Results.BadRequest(new { error = "Domain is required" });

    string normalizedDomain = body.Domain.TrimStart('@').ToLower();

    // Verify user is admin in their tenant
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    // Check domain not already in use by another tenant
    var existingDomain = await db.QueryAsync(
        "SELECT TenantId FROM dbo.TenantDomains WHERE Domain = @Domain AND IsActive = 1",
        new() { ["Domain"] = normalizedDomain });

    if (existingDomain.Rows.Count > 0)
        return Results.BadRequest(new { error = "Domain already in use by another tenant" });

    // Generate verification token (random 32-char string)
    string verificationToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
    var verificationExpiry = DateTime.UtcNow.AddDays(7);  // Token valid for 7 days

    try
    {
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.TenantDomains (TenantId, Domain, IsVerified, VerificationToken, VerificationTokenExpiry, CreatedBy, IsActive)
              VALUES (@TenantId, @Domain, 0, @Token, @Expiry, @UserId, 1)",
            new()
            {
                ["TenantId"] = parsedTenantId,
                ["Domain"] = normalizedDomain,
                ["Token"] = verificationToken,
                ["Expiry"] = verificationExpiry,
                ["UserId"] = parsedUserId
            });

        return Results.Created($"/api/tenant/verify-domain-status?domain={normalizedDomain}", new
        {
            domain = normalizedDomain,
            verificationToken,
            verificationTokenExpiry = verificationExpiry,
            message = "Domain added. Please verify ownership using the token."
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to add domain: {ex.Message}" });
    }
})
.WithName("AddDomain")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/tenant/verify-domain - Verify domain ownership via DNS TXT record
app.MapPost("/api/tenant/verify-domain", async (HttpContext ctx, VerifyDomainRequest body, DatabaseService db) =>
{
    if (string.IsNullOrWhiteSpace(body.Domain) || string.IsNullOrWhiteSpace(body.VerificationToken))
        return Results.BadRequest(new { error = "Domain and verification token required" });

    string normalizedDomain = body.Domain.TrimStart('@').ToLower();

    var result = await db.QueryAsync(
        @"SELECT Id, TenantId, VerificationToken, VerificationTokenExpiry, IsVerified
          FROM dbo.TenantDomains
          WHERE Domain = @Domain AND IsActive = 1",
        new() { ["Domain"] = normalizedDomain });

    if (result.Rows.Count == 0)
        return Results.NotFound(new { error = "Domain not found" });

    int domainId = (int)result.Rows[0]["Id"];
    string storedToken = result.Rows[0]["VerificationToken"]?.ToString() ?? "";
    object expiry = result.Rows[0]["VerificationTokenExpiry"];
    bool isVerified = (bool)result.Rows[0]["IsVerified"];

    if (isVerified)
        return Results.BadRequest(new { error = "Domain already verified" });

    // Check token expiry
    if (expiry == null || DateTime.UtcNow > (DateTime)expiry)
        return Results.BadRequest(new { error = "Verification token has expired" });

    // Verify token matches
    if (storedToken != body.VerificationToken)
        return Results.BadRequest(new { error = "Invalid verification token" });

    try
    {
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.TenantDomains
              SET IsVerified = 1, VerifiedAt = GETUTCDATE(), VerificationToken = NULL, VerificationTokenExpiry = NULL
              WHERE Id = @Id",
            new() { ["Id"] = domainId });

        // Log verification in audit trail
        int userId = int.TryParse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
        string ipAddress = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.DomainVerifications (TenantDomainId, VerificationType, VerificationMethod, IsSuccessful, AttemptedBy, IpAddress)
              VALUES (@DomainId, 'DNS', 'Token Verification', 1, @UserId, @IpAddress)",
            new() { ["DomainId"] = domainId, ["UserId"] = userId, ["IpAddress"] = ipAddress });

        return Results.Ok(new
        {
            domain = normalizedDomain,
            isVerified = true,
            message = "Domain verified successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Verification failed: {ex.Message}" });
    }
})
.WithName("VerifyDomain")
// .WithOpenApi()  // DISABLED - method not available
.AllowAnonymous();  // Allow domain verification without auth for initial setup

// GET /api/tenant/domains - Admin: List all domains for current tenant
app.MapGet("/api/tenant/domains", async (HttpContext ctx, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId))
        return Results.Unauthorized();

    var result = await db.QueryAsync(
        @"SELECT Id, Domain, IsVerified, IsActive, CreatedAt, VerifiedAt
          FROM dbo.TenantDomains
          WHERE TenantId = @TenantId AND IsActive = 1
          ORDER BY CreatedAt DESC",
        new() { ["TenantId"] = parsedTenantId });

    var domains = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (int)row["Id"],
        domain = (string)row["Domain"],
        isVerified = (bool)row["IsVerified"],
        createdAt = row["CreatedAt"],
        verifiedAt = row["VerifiedAt"]
    }).ToList();

    return Results.Ok(new { domains, count = domains.Count });
})
.WithName("ListDomains")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// DELETE /api/tenant/domains/{id} - Admin: Remove domain from tenant
app.MapDelete("/api/tenant/domains/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    // Verify domain belongs to tenant
    var domainCheck = await db.QueryAsync(
        "SELECT TenantId FROM dbo.TenantDomains WHERE Id = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = parsedTenantId });

    if (domainCheck.Rows.Count == 0)
        return Results.NotFound();

    try
    {
        await db.ExecuteNonQueryAsync(
            "UPDATE dbo.TenantDomains SET IsActive = 0, UpdatedAt = GETUTCDATE(), UpdatedBy = @UserId WHERE Id = @Id",
            new() { ["Id"] = id, ["UserId"] = parsedUserId });

        return Results.Ok(new { message = "Domain removed successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to remove domain: {ex.Message}" });
    }
})
.WithName("RemoveDomain")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Approval Permissions Management (Phase 1 Week 2) ──────────────────────────────────────
// Fine-grained approval authority with threshold-based routing

// POST /api/approval/permissions - Admin: Create new approval permission
app.MapPost("/api/approval/permissions", async (HttpContext ctx, CreateApprovalPermissionRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    // Validate either RoleId or UserId is provided (not both)
    if (string.IsNullOrWhiteSpace(body.RoleId) && body.UserId <= 0)
        return Results.BadRequest(new { error = "Either RoleId or UserId must be specified" });

    if (!string.IsNullOrWhiteSpace(body.RoleId) && body.UserId > 0)
        return Results.BadRequest(new { error = "Only one of RoleId or UserId can be specified" });

    // If UserId specified, verify user belongs to tenant
    if (body.UserId > 0)
    {
        var userCheck = await db.QueryAsync(
            "SELECT TenantId FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
            new() { ["UserId"] = body.UserId, ["TenantId"] = parsedTenantId });

        if (userCheck.Rows.Count == 0)
            return Results.BadRequest(new { error = "User not found in tenant" });
    }

    // Validate threshold logic
    if (body.MinThreshold.HasValue && body.MaxThreshold.HasValue && body.MinThreshold > body.MaxThreshold)
        return Results.BadRequest(new { error = "MinThreshold cannot be greater than MaxThreshold" });

    try
    {
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ApprovalPermissions (TenantId, RoleId, UserId, ModuleType, ApprovalLevel, MinThreshold, MaxThreshold, CanDelegate, CanReject, CanComment, CreatedBy, IsActive)
              VALUES (@TenantId, @RoleId, @UserId, @ModuleType, @Level, @MinThreshold, @MaxThreshold, @CanDelegate, @CanReject, @CanComment, @CreatedBy, 1)",
            new()
            {
                ["TenantId"] = parsedTenantId,
                ["RoleId"] = (object?)body.RoleId ?? DBNull.Value,
                ["UserId"] = body.UserId > 0 ? body.UserId : DBNull.Value,
                ["ModuleType"] = body.ModuleType,
                ["Level"] = body.ApprovalLevel,
                ["MinThreshold"] = body.MinThreshold.HasValue ? (object)body.MinThreshold : DBNull.Value,
                ["MaxThreshold"] = body.MaxThreshold.HasValue ? (object)body.MaxThreshold : DBNull.Value,
                ["CanDelegate"] = body.CanDelegate ?? true,
                ["CanReject"] = body.CanReject ?? true,
                ["CanComment"] = body.CanComment ?? true,
                ["CreatedBy"] = parsedUserId
            });

        return Results.Created("/api/approval/permissions", new { message = "Permission created successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to create permission: {ex.Message}" });
    }
})
.WithName("CreateApprovalPermission")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/approval/permissions - Admin: List permissions for tenant
app.MapGet("/api/approval/permissions", async (HttpContext ctx, DatabaseService db, string? moduleType = null) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId))
        return Results.Unauthorized();

    var query = @"SELECT Id, RoleId, UserId, ModuleType, ApprovalLevel, MinThreshold, MaxThreshold,
                        CanDelegate, CanReject, CanComment, IsActive, CreatedAt
                 FROM dbo.ApprovalPermissions
                 WHERE TenantId = @TenantId AND IsActive = 1";

    var param = new Dictionary<string, object?> { ["TenantId"] = parsedTenantId };

    if (!string.IsNullOrWhiteSpace(moduleType))
    {
        query += " AND ModuleType = @ModuleType";
        param["ModuleType"] = moduleType;
    }

    query += " ORDER BY ApprovalLevel, ModuleType";

    var result = await db.QueryAsync(query, param);

    var permissions = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (int)row["Id"],
        roleId = row["RoleId"]?.ToString(),
        userId = row["UserId"] != DBNull.Value ? (int?)row["UserId"] : null,
        moduleType = (string)row["ModuleType"],
        approvalLevel = (int)row["ApprovalLevel"],
        minThreshold = row["MinThreshold"] != DBNull.Value ? (decimal?)row["MinThreshold"] : null,
        maxThreshold = row["MaxThreshold"] != DBNull.Value ? (decimal?)row["MaxThreshold"] : null,
        canDelegate = (bool)row["CanDelegate"],
        canReject = (bool)row["CanReject"],
        canComment = (bool)row["CanComment"],
        createdAt = row["CreatedAt"]
    }).ToList();

    return Results.Ok(new { permissions, count = permissions.Count });
})
.WithName("ListApprovalPermissions")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// PUT /api/approval/permissions/{id} - Admin: Update permission
app.MapPut("/api/approval/permissions/{id:int}", async (int id, HttpContext ctx, UpdateApprovalPermissionRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    // Verify permission exists and belongs to tenant
    var permCheck = await db.QueryAsync(
        "SELECT Id FROM dbo.ApprovalPermissions WHERE Id = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = parsedTenantId });

    if (permCheck.Rows.Count == 0)
        return Results.NotFound();

    // Validate thresholds if provided
    if (body.MinThreshold.HasValue && body.MaxThreshold.HasValue && body.MinThreshold > body.MaxThreshold)
        return Results.BadRequest(new { error = "MinThreshold cannot be greater than MaxThreshold" });

    try
    {
        var updates = new List<string>();
        var @params = new Dictionary<string, object?> { ["Id"] = id };

        if (body.MinThreshold.HasValue)
        {
            updates.Add("MinThreshold = @MinThreshold");
            @params["MinThreshold"] = body.MinThreshold;
        }
        if (body.MaxThreshold.HasValue)
        {
            updates.Add("MaxThreshold = @MaxThreshold");
            @params["MaxThreshold"] = body.MaxThreshold;
        }
        if (body.CanDelegate.HasValue)
        {
            updates.Add("CanDelegate = @CanDelegate");
            @params["CanDelegate"] = body.CanDelegate;
        }
        if (body.CanReject.HasValue)
        {
            updates.Add("CanReject = @CanReject");
            @params["CanReject"] = body.CanReject;
        }

        if (updates.Count == 0)
            return Results.BadRequest(new { error = "No fields to update" });

        updates.Add("UpdatedAt = GETUTCDATE()");
        updates.Add("UpdatedBy = @UpdatedBy");
        @params["UpdatedBy"] = parsedUserId;

        string query = "UPDATE dbo.ApprovalPermissions SET " + string.Join(", ", updates) + " WHERE Id = @Id";

        await db.ExecuteNonQueryAsync(query, @params);

        return Results.Ok(new { message = "Permission updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to update permission: {ex.Message}" });
    }
})
.WithName("UpdateApprovalPermission")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// DELETE /api/approval/permissions/{id} - Admin: Revoke permission
app.MapDelete("/api/approval/permissions/{id:int}", async (int id, HttpContext ctx, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    // Verify permission exists
    var permCheck = await db.QueryAsync(
        "SELECT Id FROM dbo.ApprovalPermissions WHERE Id = @Id AND TenantId = @TenantId",
        new() { ["Id"] = id, ["TenantId"] = parsedTenantId });

    if (permCheck.Rows.Count == 0)
        return Results.NotFound();

    try
    {
        await db.ExecuteNonQueryAsync(
            "UPDATE dbo.ApprovalPermissions SET IsActive = 0, UpdatedAt = GETUTCDATE(), UpdatedBy = @UserId WHERE Id = @Id",
            new() { ["Id"] = id, ["UserId"] = parsedUserId });

        // Log to audit trail
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ApprovalPermissionAudit (TenantId, PermissionId, ChangeType, ChangedAt, ChangedBy, IsActive)
              VALUES (@TenantId, @PermId, 'REVOKE', GETUTCDATE(), @UserId, 1)",
            new() { ["TenantId"] = parsedTenantId, ["PermId"] = id, ["UserId"] = parsedUserId });

        return Results.Ok(new { message = "Permission revoked successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to revoke permission: {ex.Message}" });
    }
})
.WithName("RevokeApprovalPermission")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/approval/check-permission - Check if current user has approval authority
app.MapPost("/api/approval/check-permission", async (HttpContext ctx, CheckApprovalPermissionRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;
    var userRole = ctx.User.FindFirst(ClaimTypes.Role)?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Check for matching permission (first by user, then by role)
    var permResult = await db.QueryAsync(
        @"SELECT ap.Id, ap.MinThreshold, ap.MaxThreshold, ap.CanDelegate, ap.CanReject, ap.CanComment, ap.ApprovalLevel
          FROM dbo.ApprovalPermissions ap
          WHERE ap.TenantId = @TenantId
            AND ap.ModuleType = @ModuleType
            AND ap.ApprovalLevel = @Level
            AND ap.IsActive = 1
            AND (
              (ap.UserId = @UserId)
              OR (ap.RoleId = @Role)
            )
            AND (ap.MinThreshold IS NULL OR ap.MinThreshold <= @Amount)
            AND (ap.MaxThreshold IS NULL OR ap.MaxThreshold >= @Amount)",
        new()
        {
            ["TenantId"] = parsedTenantId,
            ["ModuleType"] = body.ModuleType,
            ["Level"] = body.ApprovalLevel,
            ["UserId"] = parsedUserId,
            ["Role"] = userRole ?? "",
            ["Amount"] = body.Amount ?? 0
        });

    if (permResult.Rows.Count == 0)
        return Results.Ok(new
        {
            hasPermission = false,
            message = "User does not have approval authority for this request"
        });

    var perm = permResult.Rows[0];
    return Results.Ok(new
    {
        hasPermission = true,
        permissionId = (int)perm["Id"],
        approvalLevel = (int)perm["ApprovalLevel"],
        canDelegate = (bool)perm["CanDelegate"],
        canReject = (bool)perm["CanReject"],
        canComment = (bool)perm["CanComment"],
        message = "User has approval authority"
    });
})
.WithName("CheckApprovalPermission")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Compliance Audit Logging (Phase 1 Week 3) ────────────────────────────────────────
// Append-only immutable audit trail for regulatory compliance

// GET /api/compliance/audit-log - View audit log (admin only)
app.MapGet("/api/compliance/audit-log", async (HttpContext ctx, DatabaseService db, int page = 1, int pageSize = 50) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    int skip = (page - 1) * pageSize;

    var result = await db.QueryAsync(
        @"SELECT Id, EntityType, EntityId, Action, UserId, Status, Reason, IpAddress, AuditedAt
          FROM dbo.ComplianceAuditLog
          WHERE TenantId = @TenantId
          ORDER BY AuditedAt DESC
          OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
        new() { ["TenantId"] = parsedTenantId, ["Skip"] = skip, ["Take"] = pageSize });

    var totalResult = await db.QueryAsync(
        "SELECT COUNT(*) as total FROM dbo.ComplianceAuditLog WHERE TenantId = @TenantId",
        new() { ["TenantId"] = parsedTenantId });

    int total = (int)totalResult.Rows[0]["total"];

    var logs = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (long)row["Id"],
        entityType = (string)row["EntityType"],
        entityId = (int)row["EntityId"],
        action = (string)row["Action"],
        userId = row["UserId"] != DBNull.Value ? (int?)row["UserId"] : null,
        status = (string)row["Status"],
        reason = row["Reason"]?.ToString(),
        ipAddress = row["IpAddress"]?.ToString(),
        auditedAt = row["AuditedAt"]
    }).ToList();

    return Results.Ok(new
    {
        logs,
        pagination = new { page, pageSize, total, totalPages = (total + pageSize - 1) / pageSize }
    });
})
.WithName("GetAuditLog")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/compliance/audit-log/entity/{entityType}/{entityId} - View audit history for specific entity
app.MapGet("/api/compliance/audit-log/entity/{entityType}/{entityId:int}", async (string entityType, int entityId, HttpContext ctx, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify user can access this entity (basic tenant isolation)
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    var result = await db.QueryAsync(
        @"SELECT Id, Action, UserId, OldValues, NewValues, ChangedFields, Reason, IpAddress, Status, AuditedAt
          FROM dbo.ComplianceAuditLog
          WHERE TenantId = @TenantId AND EntityType = @EntityType AND EntityId = @EntityId
          ORDER BY AuditedAt DESC",
        new() { ["TenantId"] = parsedTenantId, ["EntityType"] = entityType, ["EntityId"] = entityId });

    var history = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (long)row["Id"],
        action = (string)row["Action"],
        userId = row["UserId"] != DBNull.Value ? (int?)row["UserId"] : null,
        oldValues = row["OldValues"]?.ToString(),
        newValues = row["NewValues"]?.ToString(),
        changedFields = row["ChangedFields"]?.ToString(),
        reason = row["Reason"]?.ToString(),
        ipAddress = row["IpAddress"]?.ToString(),
        status = (string)row["Status"],
        auditedAt = row["AuditedAt"]
    }).ToList();

    return Results.Ok(new { entity = $"{entityType}#{entityId}", history });
})
.WithName("GetEntityAuditHistory")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/compliance/security-events - View security events (admin only)
app.MapGet("/api/compliance/security-events", async (HttpContext ctx, DatabaseService db, string? severity = null) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    string query = @"SELECT Id, EventType, Severity, UserId, Description, AffectedRecords, IpAddress, OccurredAt, IsResolved
                   FROM dbo.SecurityAuditEvents
                   WHERE TenantId = @TenantId";

    var @params = new Dictionary<string, object?> { ["TenantId"] = parsedTenantId };

    if (!string.IsNullOrWhiteSpace(severity))
    {
        query += " AND Severity = @Severity";
        @params["Severity"] = severity;
    }

    query += " ORDER BY OccurredAt DESC";

    var result = await db.QueryAsync(query, @params);

    var events = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (int)row["Id"],
        eventType = (string)row["EventType"],
        severity = (string)row["Severity"],
        userId = row["UserId"] != DBNull.Value ? (int?)row["UserId"] : null,
        description = (string)row["Description"],
        affectedRecords = (int?)row["AffectedRecords"],
        ipAddress = row["IpAddress"]?.ToString(),
        occurredAt = row["OccurredAt"],
        isResolved = (bool)row["IsResolved"]
    }).ToList();

    return Results.Ok(new { events, count = events.Count });
})
.WithName("GetSecurityEvents")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/compliance/security-events - Admin: Record security event
app.MapPost("/api/compliance/security-events", async (HttpContext ctx, RecordSecurityEventRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    try
    {
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.SecurityAuditEvents (TenantId, EventType, Severity, UserId, Description, AffectedRecords, IpAddress, IsResolved)
              VALUES (@TenantId, @EventType, @Severity, @UserId, @Description, @AffectedRecords, @IpAddress, 0)",
            new()
            {
                ["TenantId"] = parsedTenantId,
                ["EventType"] = body.EventType,
                ["Severity"] = body.Severity,
                ["UserId"] = body.UserId.HasValue ? (object)body.UserId : DBNull.Value,
                ["Description"] = body.Description,
                ["AffectedRecords"] = body.AffectedRecords.HasValue ? (object)body.AffectedRecords : DBNull.Value,
                ["IpAddress"] = body.IpAddress ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            });

        return Results.Created("/api/compliance/security-events", new { message = "Security event recorded" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to record event: {ex.Message}" });
    }
})
.WithName("RecordSecurityEvent")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// PUT /api/compliance/security-events/{id} - Admin: Investigate security event
app.MapPut("/api/compliance/security-events/{id:int}", async (int id, HttpContext ctx, InvestigateSecurityEventRequest body, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    try
    {
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.SecurityAuditEvents
              SET InvestigatedAt = GETUTCDATE(), InvestigationNotes = @Notes, IsResolved = @IsResolved
              WHERE Id = @Id AND TenantId = @TenantId",
            new()
            {
                ["Id"] = id,
                ["TenantId"] = parsedTenantId,
                ["Notes"] = body.Notes,
                ["IsResolved"] = body.IsResolved
            });

        return Results.Ok(new { message = "Security event investigation updated" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to update event: {ex.Message}" });
    }
})
.WithName("InvestigateSecurityEvent")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Rate Limiting Management (Phase 1 Week 4) ────────────────────────────────────────

// GET /api/security/rate-limiting/violations - Admin: View rate limit violations
app.MapGet("/api/security/rate-limiting/violations", async (HttpContext ctx, DatabaseService db, int page = 1, int pageSize = 50) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    int skip = (page - 1) * pageSize;

    var result = await db.QueryAsync(
        @"SELECT Id, Identifier, IdentifierType, EndpointPattern, SuspicionLevel, IsAutoBlocked, BlockedUntil, ViolationAt
          FROM dbo.RateLimitingViolations
          ORDER BY ViolationAt DESC
          OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
        new() { ["Skip"] = skip, ["Take"] = pageSize });

    var totalResult = await db.QueryAsync(
        "SELECT COUNT(*) as total FROM dbo.RateLimitingViolations");

    int total = (int)totalResult.Rows[0]["total"];

    var violations = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (int)row["Id"],
        identifier = (string)row["Identifier"],
        identifierType = (string)row["IdentifierType"],
        endpointPattern = row["EndpointPattern"]?.ToString(),
        suspicionLevel = (string)row["SuspicionLevel"],
        isAutoBlocked = (bool)row["IsAutoBlocked"],
        blockedUntil = row["BlockedUntil"],
        violationAt = row["ViolationAt"]
    }).ToList();

    return Results.Ok(new
    {
        violations,
        pagination = new { page, pageSize, total, totalPages = (total + pageSize - 1) / pageSize }
    });
})
.WithName("GetRateLimitViolations")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// PUT /api/security/rate-limiting/violations/{id}/unblock - Admin: Unblock IP/User
app.MapPut("/api/security/rate-limiting/violations/{id:int}/unblock", async (int id, HttpContext ctx, DatabaseService db) =>
{
    var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
    var userId = ctx.User.FindFirst("UserId")?.Value;

    if (!int.TryParse(tenantId, out int parsedTenantId) || !int.TryParse(userId, out int parsedUserId))
        return Results.Unauthorized();

    // Verify admin
    var adminCheck = await db.QueryAsync(
        "SELECT IsAdmin FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
        new() { ["UserId"] = parsedUserId, ["TenantId"] = parsedTenantId });

    if (adminCheck.Rows.Count == 0 || !(bool)adminCheck.Rows[0]["IsAdmin"])
        return Results.Forbid();

    try
    {
        await db.ExecuteNonQueryAsync(
            "UPDATE dbo.RateLimitingViolations SET IsAutoBlocked = 0, BlockedUntil = NULL WHERE Id = @Id",
            new() { ["Id"] = id });

        return Results.Ok(new { message = "Rate limit block removed" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to unblock: {ex.Message}" });
    }
})
.WithName("UnblockRateLimitViolation")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Product Admin Module (Phase 2 Weeks 5-6) ────────────────────────────────────────
// Client management, billing configuration, and invoice tracking
// Protected: Only accessible to MyDesk Super Admin users

// Helper: Verify user is MyDesk Super Admin (only product admins can manage clients)
async Task<bool> IsMyDeskSuperAdminAsync(HttpContext ctx, DatabaseService db)
{
    var userId = ctx.User.FindFirst("UserId")?.Value;
    if (!int.TryParse(userId, out int parsedUserId))
        return false;

    var result = await db.QueryAsync(
        "SELECT UserTypeId FROM dbo.Users WHERE UserId = @UserId",
        new() { ["UserId"] = parsedUserId });

    // UserTypeId 5 = Super Administrator (MyDesk platform admin)
    return result.Rows.Count > 0 && (int)result.Rows[0]["UserTypeId"] == 5;
}

// GET /api/product-admin/clients - List all client tenants
app.MapGet("/api/product-admin/clients", async (HttpContext ctx, DatabaseService db, int page = 1, int pageSize = 50) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    int skip = (page - 1) * pageSize;

    var result = await db.QueryAsync(
        @"SELECT t.TenantId, t.Name, t.Code, t.IsActive, cbc.BillingModel, cbc.Status as BillingStatus,
                 (SELECT COUNT(*) FROM dbo.Users u WHERE u.TenantId = t.TenantId) as UserCount,
                 t.CreatedAt
          FROM dbo.Tenants t
          LEFT JOIN dbo.ClientBillingConfig cbc ON cbc.TenantId = t.TenantId
          WHERE t.IsActive = 1 AND t.TenantId > 1  -- Exclude MyDesk platform tenant (ID=1)
          ORDER BY t.CreatedAt DESC
          OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
        new() { ["Skip"] = skip, ["Take"] = pageSize });

    var totalResult = await db.QueryAsync(
        @"SELECT COUNT(*) as total FROM dbo.Tenants
          WHERE IsActive = 1 AND TenantId > 1");

    int total = (int)totalResult.Rows[0]["total"];

    var clients = result.Rows.Cast<DataRow>().Select(row => new
    {
        tenantId = (int)row["TenantId"],
        name = (string)row["Name"],
        code = (string)row["Code"],
        isActive = (bool)row["IsActive"],
        billingModel = row["BillingModel"]?.ToString(),
        billingStatus = row["BillingStatus"]?.ToString(),
        userCount = (int)row["UserCount"],
        createdAt = row["CreatedAt"]
    }).ToList();

    return Results.Ok(new
    {
        clients,
        pagination = new { page, pageSize, total, totalPages = (total + pageSize - 1) / pageSize }
    });
})
.WithName("ListClients")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/product-admin/clients/{tenantId}/billing - View client billing configuration
app.MapGet("/api/product-admin/clients/{tenantId:int}/billing", async (int tenantId, HttpContext ctx, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var result = await db.QueryAsync(
        @"SELECT Id, BillingModel, BillingContactEmail, Currency, TaxPercentage, Status, CycleStartDay, CycleStartMonth
          FROM dbo.ClientBillingConfig
          WHERE TenantId = @TenantId",
        new() { ["TenantId"] = tenantId });

    if (result.Rows.Count == 0)
        return Results.NotFound();

    var config = result.Rows[0];
    return Results.Ok(new
    {
        billingModel = (string)config["BillingModel"],
        billingContactEmail = config["BillingContactEmail"]?.ToString(),
        currency = (string)config["Currency"],
        taxPercentage = (decimal?)config["TaxPercentage"],
        status = (string)config["Status"],
        cycleStartDay = (int?)config["CycleStartDay"],
        cycleStartMonth = (int?)config["CycleStartMonth"]
    });
})
.WithName("GetClientBillingConfig")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/product-admin/clients/{tenantId}/billing - Update client billing configuration
app.MapPost("/api/product-admin/clients/{tenantId:int}/billing", async (int tenantId, HttpContext ctx, UpdateBillingConfigRequest body, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

    // Verify billing model is valid
    var validModels = new[] { "MONTHLY_ADVANCE", "YEARLY_ADVANCE", "PAY_AS_YOU_GO", "FLAT_RATE" };
    if (!validModels.Contains(body.BillingModel))
        return Results.BadRequest(new { error = "Invalid billing model" });

    try
    {
        // Check if config exists
        var existingResult = await db.QueryAsync(
            "SELECT Id FROM dbo.ClientBillingConfig WHERE TenantId = @TenantId",
            new() { ["TenantId"] = tenantId });

        if (existingResult.Rows.Count == 0)
        {
            // Create new config
            await db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.ClientBillingConfig (TenantId, BillingModel, BillingContactEmail, Currency, TaxPercentage,
                    CycleStartDay, CycleStartMonth, Status, CreatedBy)
                  VALUES (@TenantId, @Model, @Email, @Currency, @Tax, @StartDay, @StartMonth, 'ACTIVE', @UserId)",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Model"] = body.BillingModel,
                    ["Email"] = body.BillingContactEmail ?? (object)DBNull.Value,
                    ["Currency"] = body.Currency ?? "AUD",
                    ["Tax"] = body.TaxPercentage ?? 10,
                    ["StartDay"] = body.CycleStartDay ?? 1,
                    ["StartMonth"] = body.CycleStartMonth ?? 1,
                    ["UserId"] = userId
                });

            // Log change
            await db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.ClientBillingHistory (TenantId, ChangeType, NewValues, Reason, ChangedBy)
                  VALUES (@TenantId, 'CONFIG_CHANGE', @Values, @Reason, @UserId)",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Values"] = $"BillingModel={body.BillingModel}",
                    ["Reason"] = "Initial billing configuration created",
                    ["UserId"] = userId
                });

            return Results.Created($"/api/product-admin/clients/{tenantId}/billing",
                new { message = "Billing configuration created" });
        }
        else
        {
            // Update existing config
            await db.ExecuteNonQueryAsync(
                @"UPDATE dbo.ClientBillingConfig
                  SET BillingModel = @Model, BillingContactEmail = @Email, Currency = @Currency,
                      TaxPercentage = @Tax, CycleStartDay = @StartDay, CycleStartMonth = @StartMonth,
                      UpdatedAt = GETUTCDATE(), UpdatedBy = @UserId
                  WHERE TenantId = @TenantId",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Model"] = body.BillingModel,
                    ["Email"] = body.BillingContactEmail ?? (object)DBNull.Value,
                    ["Currency"] = body.Currency ?? "AUD",
                    ["Tax"] = body.TaxPercentage ?? 10,
                    ["StartDay"] = body.CycleStartDay ?? 1,
                    ["StartMonth"] = body.CycleStartMonth ?? 1,
                    ["UserId"] = userId
                });

            // Log change
            await db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.ClientBillingHistory (TenantId, ChangeType, NewValues, Reason, ChangedBy)
                  VALUES (@TenantId, 'CONFIG_CHANGE', @Values, @Reason, @UserId)",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Values"] = $"BillingModel={body.BillingModel}",
                    ["Reason"] = "Billing configuration updated",
                    ["UserId"] = userId
                });

            return Results.Ok(new { message = "Billing configuration updated" });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to update billing config: {ex.Message}" });
    }
})
.WithName("UpdateBillingConfig")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/product-admin/invoices - List all invoices (with optional filtering)
app.MapGet("/api/product-admin/invoices", async (HttpContext ctx, DatabaseService db,
    int? tenantId = null, string? status = null, int page = 1, int pageSize = 50) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    int skip = (page - 1) * pageSize;

    string query = @"SELECT Id, TenantId, InvoiceNumber, InvoiceDate, DueDate, TotalAmount,
                           Status, PaidAt, CreatedAt
                    FROM dbo.ClientInvoice
                    WHERE 1=1";

    var @params = new Dictionary<string, object?>();

    if (tenantId.HasValue)
    {
        query += " AND TenantId = @TenantId";
        @params["TenantId"] = tenantId;
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        query += " AND Status = @Status";
        @params["Status"] = status;
    }

    query += " ORDER BY InvoiceDate DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
    @params["Skip"] = skip;
    @params["Take"] = pageSize;

    var result = await db.QueryAsync(query, @params);

    // Get total count
    string countQuery = "SELECT COUNT(*) as total FROM dbo.ClientInvoice WHERE 1=1";
    if (tenantId.HasValue)
        countQuery += " AND TenantId = @TenantId";
    if (!string.IsNullOrWhiteSpace(status))
        countQuery += " AND Status = @Status";

    var countResult = await db.QueryAsync(countQuery, @params);
    int total = (int)countResult.Rows[0]["total"];

    var invoices = result.Rows.Cast<DataRow>().Select(row => new
    {
        id = (int)row["Id"],
        tenantId = (int)row["TenantId"],
        invoiceNumber = (string)row["InvoiceNumber"],
        invoiceDate = row["InvoiceDate"],
        dueDate = row["DueDate"],
        totalAmount = (decimal)row["TotalAmount"],
        status = (string)row["Status"],
        paidAt = row["PaidAt"],
        createdAt = row["CreatedAt"]
    }).ToList();

    return Results.Ok(new
    {
        invoices,
        pagination = new { page, pageSize, total, totalPages = (total + pageSize - 1) / pageSize }
    });
})
.WithName("ListInvoices")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/product-admin/invoices/{invoiceId} - Get invoice details
app.MapGet("/api/product-admin/invoices/{invoiceId:int}", async (int invoiceId, HttpContext ctx, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var result = await db.QueryAsync(
        @"SELECT Id, TenantId, InvoiceNumber, InvoiceDate, DueDate, BillingPeriodStart, BillingPeriodEnd,
                 BaseAmount, UsageAmount, DiscountAmount, TaxAmount, TotalAmount, Status, PaidAt,
                 PaymentMethod, PaymentReference, BillingContactEmail, Notes
          FROM dbo.ClientInvoice
          WHERE Id = @Id",
        new() { ["Id"] = invoiceId });

    if (result.Rows.Count == 0)
        return Results.NotFound();

    var inv = result.Rows[0];
    return Results.Ok(new
    {
        id = (int)inv["Id"],
        tenantId = (int)inv["TenantId"],
        invoiceNumber = (string)inv["InvoiceNumber"],
        invoiceDate = inv["InvoiceDate"],
        dueDate = inv["DueDate"],
        billingPeriodStart = inv["BillingPeriodStart"],
        billingPeriodEnd = inv["BillingPeriodEnd"],
        baseAmount = (decimal)inv["BaseAmount"],
        usageAmount = (decimal)inv["UsageAmount"],
        discountAmount = (decimal)inv["DiscountAmount"],
        taxAmount = (decimal)inv["TaxAmount"],
        totalAmount = (decimal)inv["TotalAmount"],
        status = (string)inv["Status"],
        paidAt = inv["PaidAt"],
        paymentMethod = inv["PaymentMethod"]?.ToString(),
        paymentReference = inv["PaymentReference"]?.ToString(),
        notes = inv["Notes"]?.ToString()
    });
})
.WithName("GetInvoiceDetails")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// PUT /api/product-admin/invoices/{invoiceId}/mark-paid - Mark invoice as paid
app.MapPut("/api/product-admin/invoices/{invoiceId:int}/mark-paid", async (int invoiceId, HttpContext ctx, MarkInvoicePaidRequest body, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

    try
    {
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.ClientInvoice
              SET Status = 'PAID', PaidAt = GETUTCDATE(), PaidAmount = @Amount,
                  PaymentMethod = @Method, PaymentReference = @Reference, UpdatedBy = @UserId, UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            new()
            {
                ["Id"] = invoiceId,
                ["Amount"] = body.Amount,
                ["Method"] = body.PaymentMethod ?? (object)DBNull.Value,
                ["Reference"] = body.PaymentReference ?? (object)DBNull.Value,
                ["UserId"] = userId
            });

        return Results.Ok(new { message = "Invoice marked as paid" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to mark invoice as paid: {ex.Message}" });
    }
})
.WithName("MarkInvoicePaid")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Client Onboarding Wizard (Phase 2 Weeks 7-8) ────────────────────────────────────────
// 6-step wizard for creating and configuring new client tenants

// POST /api/product-admin/onboarding/start - Initiate wizard session
app.MapPost("/api/product-admin/onboarding/start", async (HttpContext ctx, StartOnboardingRequest body, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

    // Validate email format
    if (string.IsNullOrWhiteSpace(body.AdminEmail) || !body.AdminEmail.Contains("@"))
        return Results.BadRequest(new { error = "Invalid admin email" });

    try
    {
        // Generate unique session token
        string sessionToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));

        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ClientOnboardingSession
              (SessionToken, AdminUserId, CurrentStep, AdminName, AdminEmail, Status, StartedAt)
              VALUES (@Token, @UserId, 1, @AdminName, @AdminEmail, 'IN_PROGRESS', GETUTCDATE())",
            new()
            {
                ["Token"] = sessionToken,
                ["UserId"] = userId,
                ["AdminName"] = body.AdminName,
                ["AdminEmail"] = body.AdminEmail
            });

        return Results.Created($"/api/product-admin/onboarding/{sessionToken}", new
        {
            sessionToken,
            currentStep = 1,
            message = "Onboarding session created"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to start onboarding: {ex.Message}" });
    }
})
.WithName("StartOnboarding")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/product-admin/onboarding/{sessionToken} - Get wizard session state
app.MapGet("/api/product-admin/onboarding/{sessionToken}", async (string sessionToken, HttpContext ctx, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var result = await db.QueryAsync(
        @"SELECT CurrentStep, TenantName, TenantCode, Domain, ApprovalWorkflowTemplate,
                 BillingModel, InitialUserSeats, AdminName, AdminEmail, Status
          FROM dbo.ClientOnboardingSession
          WHERE SessionToken = @Token AND Status IN ('IN_PROGRESS', 'COMPLETED')",
        new() { ["Token"] = sessionToken });

    if (result.Rows.Count == 0)
        return Results.NotFound();

    var session = result.Rows[0];
    return Results.Ok(new
    {
        currentStep = (int)session["CurrentStep"],
        tenantName = session["TenantName"]?.ToString(),
        tenantCode = session["TenantCode"]?.ToString(),
        domain = session["Domain"]?.ToString(),
        approvalWorkflow = session["ApprovalWorkflowTemplate"]?.ToString(),
        billingModel = session["BillingModel"]?.ToString(),
        userSeats = session["InitialUserSeats"],
        adminName = (string)session["AdminName"],
        adminEmail = (string)session["AdminEmail"],
        status = (string)session["Status"]
    });
})
.WithName("GetOnboardingSession")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/product-admin/onboarding/{sessionToken}/steps/{step} - Submit step data
app.MapPost("/api/product-admin/onboarding/{sessionToken}/steps/{step:int}",
    async (string sessionToken, int step, HttpContext ctx, OnboardingStepRequest body, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    if (step < 1 || step > 6)
        return Results.BadRequest(new { error = "Invalid step number (1-6)" });

    try
    {
        // Get current session
        var sessionResult = await db.QueryAsync(
            "SELECT Id, CurrentStep FROM dbo.ClientOnboardingSession WHERE SessionToken = @Token AND Status = 'IN_PROGRESS'",
            new() { ["Token"] = sessionToken });

        if (sessionResult.Rows.Count == 0)
            return Results.NotFound();

        int sessionId = (int)sessionResult.Rows[0]["Id"];
        int currentStep = (int)sessionResult.Rows[0]["CurrentStep"];

        // Validate step sequence
        if (step != currentStep)
            return Results.BadRequest(new { error = $"Expected step {currentStep}, got step {step}" });

        // Step-specific validation and data update
        string updateQuery = "";
        var @params = new Dictionary<string, object?> { ["SessionId"] = sessionId, ["Token"] = sessionToken };

        switch (step)
        {
            case 1:  // Basic info
                if (string.IsNullOrWhiteSpace(body.TenantName) || string.IsNullOrWhiteSpace(body.TenantCode))
                    return Results.BadRequest(new { error = "Tenant name and code required" });
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET TenantName = @TenantName, TenantCode = @TenantCode, CurrentStep = 2 WHERE Id = @SessionId";
                @params["TenantName"] = body.TenantName;
                @params["TenantCode"] = body.TenantCode;
                break;

            case 2:  // Domain
                if (string.IsNullOrWhiteSpace(body.Domain))
                    return Results.BadRequest(new { error = "Domain required" });
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET Domain = @Domain, CurrentStep = 3 WHERE Id = @SessionId";
                @params["Domain"] = body.Domain.ToLower();
                break;

            case 3:  // Approval workflow
                if (string.IsNullOrWhiteSpace(body.ApprovalWorkflow))
                    return Results.BadRequest(new { error = "Approval workflow template required" });
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET ApprovalWorkflowTemplate = @Template, CurrentStep = 4 WHERE Id = @SessionId";
                @params["Template"] = body.ApprovalWorkflow;
                break;

            case 4:  // Billing
                if (string.IsNullOrWhiteSpace(body.BillingModel))
                    return Results.BadRequest(new { error = "Billing model required" });
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET BillingModel = @Model, BillingContactEmail = @Email, CurrentStep = 5 WHERE Id = @SessionId";
                @params["Model"] = body.BillingModel;
                @params["Email"] = body.BillingContactEmail ?? (object)DBNull.Value;
                break;

            case 5:  // User seats
                if (body.UserSeats <= 0)
                    return Results.BadRequest(new { error = "User seats must be greater than 0" });
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET InitialUserSeats = @Seats, CurrentStep = 6 WHERE Id = @SessionId";
                @params["Seats"] = body.UserSeats;
                break;

            case 6:  // Confirmation (just update step)
                updateQuery = "UPDATE dbo.ClientOnboardingSession SET IsConfirmed = 1, ConfirmedAt = GETUTCDATE() WHERE Id = @SessionId";
                break;
        }

        await db.ExecuteNonQueryAsync(updateQuery, @params);

        return Results.Ok(new
        {
            stepCompleted = step,
            nextStep = step < 6 ? step + 1 : null,
            message = step == 6 ? "Wizard confirmation complete. Ready to create tenant." : $"Step {step} completed"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to save step: {ex.Message}" });
    }
})
.WithName("SubmitOnboardingStep")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/product-admin/onboarding/{sessionToken}/complete - Complete wizard and create tenant
app.MapPost("/api/product-admin/onboarding/{sessionToken}/complete", async (string sessionToken, HttpContext ctx, DatabaseService db) =>
{
    if (!await IsMyDeskSuperAdminAsync(ctx, db))
        return Results.Forbid();

    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

    try
    {
        // Get completed session
        var sessionResult = await db.QueryAsync(
            @"SELECT Id, TenantName, TenantCode, Domain, ApprovalWorkflowTemplate, BillingModel,
                     InitialUserSeats, BillingContactEmail, AdminName, AdminEmail, AdminPassword, IsConfirmed
              FROM dbo.ClientOnboardingSession
              WHERE SessionToken = @Token AND Status = 'IN_PROGRESS' AND IsConfirmed = 1 AND CurrentStep = 6",
            new() { ["Token"] = sessionToken });

        if (sessionResult.Rows.Count == 0)
            return Results.BadRequest(new { error = "Session not found or not ready for completion" });

        var session = sessionResult.Rows[0];
        int sessionId = (int)session["Id"];

        // Create new tenant
        string tenantCode = ((string)session["TenantCode"]).ToUpper();
        string tenantName = (string)session["TenantName"];

        var createTenantResult = await db.QueryAsync(
            @"INSERT INTO dbo.Tenants (Name, Code, IsActive, CreatedAt, CreatedBy, UpdatedAt)
              OUTPUT INSERTED.TenantId
              VALUES (@Name, @Code, 1, GETUTCDATE(), @UserId, GETUTCDATE())",
            new() { ["Name"] = tenantName, ["Code"] = tenantCode, ["UserId"] = userId });

        int newTenantId = (int)createTenantResult.Rows[0]["TenantId"];

        // Create admin user for the tenant
        string adminName = (string)session["AdminName"];
        string adminEmail = (string)session["AdminEmail"];

        // Default password (should be changed on first login in production)
        string initialPassword = "TempPassword123!";

        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.Users (TenantId, Name, Email, PasswordHash, IsAdmin, IsActive, CreatedAt)
              VALUES (@TenantId, @Name, @Email, @Password, 1, 1, GETUTCDATE())",
            new()
            {
                ["TenantId"] = newTenantId,
                ["Name"] = adminName,
                ["Email"] = adminEmail,
                ["Password"] = initialPassword  // In production, hash this
            });

        // Create domain mapping
        string domain = ((string)session["Domain"]).ToLower();
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.TenantDomains (TenantId, Domain, IsVerified, CreatedBy, IsActive)
              VALUES (@TenantId, @Domain, 1, @UserId, 1)",
            new() { ["TenantId"] = newTenantId, ["Domain"] = domain, ["UserId"] = userId });

        // Create billing configuration
        string billingModel = (string)session["BillingModel"];
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ClientBillingConfig (TenantId, BillingModel, BillingContactEmail, Status, CreatedBy)
              VALUES (@TenantId, @Model, @Email, 'ACTIVE', @UserId)",
            new()
            {
                ["TenantId"] = newTenantId,
                ["Model"] = billingModel,
                ["Email"] = session["BillingContactEmail"] ?? (object)DBNull.Value,
                ["UserId"] = userId
            });

        // Mark session as completed
        await db.ExecuteNonQueryAsync(
            "UPDATE dbo.ClientOnboardingSession SET TenantId = @TenantId, Status = 'COMPLETED', CompletedAt = GETUTCDATE() WHERE Id = @Id",
            new() { ["Id"] = sessionId, ["TenantId"] = newTenantId });

        // Store wizard completion snapshot
        var wizardDataJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            tenantName,
            tenantCode,
            domain,
            billingModel,
            userSeats = session["InitialUserSeats"]
        });

        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ClientOnboardingTemplate (SessionId, TenantId, WizardData, OnboardingCompletedBy)
              VALUES (@SessionId, @TenantId, @Data, @UserId)",
            new()
            {
                ["SessionId"] = sessionId,
                ["TenantId"] = newTenantId,
                ["Data"] = wizardDataJson,
                ["UserId"] = userId
            });

        return Results.Created($"/api/product-admin/clients/{newTenantId}", new
        {
            tenantId = newTenantId,
            tenantName,
            tenantCode,
            adminEmail,
            tempPassword = initialPassword,
            message = "Client tenant created successfully. Admin should change password on first login."
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Failed to complete onboarding: {ex.Message}" });
    }
})
.WithName("CompleteOnboarding")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── User Profile Photos (New Feature) ────────────────────────────────────────

// GET /api/user/profile - Get current user's profile
app.MapGet("/api/user/profile", async (HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        var result = await db.QueryAsync(
            @"SELECT UserId, Name, Email, TenantId, CurrentPhotoId
              FROM dbo.Users
              WHERE UserId = @UserId AND TenantId = @TenantId",
            new() { ["UserId"] = userId, ["TenantId"] = tenantId });

        if (result.Rows.Count == 0)
            return Results.NotFound();

        var user = result.Rows[0];
        var photoId = user["CurrentPhotoId"];
        string? photoUrl = null;

        if (photoId != DBNull.Value)
        {
            var photoResult = await db.QueryAsync(
                @"SELECT StoragePath FROM dbo.UserPhotos WHERE PhotoId = @PhotoId AND Status = 'Active'",
                new() { ["PhotoId"] = photoId });

            if (photoResult.Rows.Count > 0)
                photoUrl = (string)photoResult.Rows[0]["StoragePath"];
        }

        return Results.Ok(new
        {
            userId = (int)user["UserId"],
            name = user["Name"],
            email = user["Email"],
            photoUrl = photoUrl,
            hasPhoto = photoUrl != null
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetUserProfile")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/user/profile/photo/upload - Upload and crop photo
// TEMPORARILY DISABLED - Diagnostic: PhotoProcessingService issue
/* DISABLED
app.MapPost("/api/user/profile/photo/upload", async (HttpContext ctx, DatabaseService db, PhotoProcessingService photoService) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        var form = await ctx.Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault("photo");

        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "No photo provided" });

        using var stream = file.OpenReadStream();
        var (isValid, error) = await photoService.ValidateImageAsync(stream, file.ContentType ?? "");

        if (!isValid)
            return Results.BadRequest(new { error });

        // Reset stream position after validation
        stream.Position = 0;

        // Convert to square
        var (squareImage, dimension, contentType) = await photoService.ConvertToSquareAsync(stream, file.ContentType ?? "image/jpeg");

        // Save photo
        var storagePath = await photoService.SaveImageAsync(squareImage, tenantId.ToString(), userId.ToString(), file.FileName);

        // Store in database
        var insertResult = await db.QueryAsync(
            @"INSERT INTO dbo.UserPhotos (UserId, TenantId, OriginalFileName, OriginalContentType, OriginalSizeBytes, StoragePath, ProcessedWidth, ProcessedHeight, Status, CreatedBy)
              OUTPUT INSERTED.PhotoId
              VALUES (@UserId, @TenantId, @FileName, @ContentType, @Size, @StoragePath, @Width, @Height, 'Active', @UserId)",
            new()
            {
                ["UserId"] = userId,
                ["TenantId"] = tenantId,
                ["FileName"] = file.FileName,
                ["ContentType"] = contentType,
                ["Size"] = file.Length,
                ["StoragePath"] = storagePath,
                ["Width"] = dimension,
                ["Height"] = dimension
            });

        int photoId = (int)insertResult.Rows[0]["PhotoId"];

        // Update user's current photo
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.Users SET CurrentPhotoId = @PhotoId WHERE UserId = @UserId AND TenantId = @TenantId",
            new() { ["PhotoId"] = photoId, ["UserId"] = userId, ["TenantId"] = tenantId });

        // Log audit
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.UserPhotoAudit (TenantId, UserId, PhotoId, Action, AuditedBy)
              VALUES (@TenantId, @UserId, @PhotoId, 'Uploaded', @AuditedBy)",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId, ["PhotoId"] = photoId, ["AuditedBy"] = userId });

        return Results.Ok(new
        {
            photoId,
            photoUrl = storagePath,
            dimension,
            message = "Photo uploaded successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UploadUserPhoto")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization()
.DisableAntiforgery();
*/

// DELETE /api/user/profile/photo - Remove current photo
app.MapDelete("/api/user/profile/photo", async (HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        // Get current photo
        var photoResult = await db.QueryAsync(
            @"SELECT CurrentPhotoId FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
            new() { ["UserId"] = userId, ["TenantId"] = tenantId });

        if (photoResult.Rows.Count == 0)
            return Results.NotFound();

        var currentPhotoId = photoResult.Rows[0]["CurrentPhotoId"];

        if (currentPhotoId == DBNull.Value)
            return Results.BadRequest(new { error = "No photo to delete" });

        // Mark as deleted
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.UserPhotos SET Status = 'Deleted' WHERE PhotoId = @PhotoId",
            new() { ["PhotoId"] = currentPhotoId });

        // Clear user's photo reference
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.Users SET CurrentPhotoId = NULL WHERE UserId = @UserId AND TenantId = @TenantId",
            new() { ["UserId"] = userId, ["TenantId"] = tenantId });

        // Log audit
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.UserPhotoAudit (TenantId, UserId, PhotoId, Action, AuditedBy)
              VALUES (@TenantId, @UserId, @PhotoId, 'Deleted', @AuditedBy)",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId, ["PhotoId"] = currentPhotoId, ["AuditedBy"] = userId });

        return Results.Ok(new { message = "Photo deleted successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("DeleteUserPhoto")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Expense Receipt Photos (New Feature) ──────────────────────────────────────

// POST /api/expenses/{expenseId}/receipt/upload - Upload receipt photo
// TEMPORARILY DISABLED - Diagnostic: PhotoProcessingService issue
/* DISABLED
app.MapPost("/api/expenses/{expenseId:int}/receipt/upload", async (int expenseId, HttpContext ctx, DatabaseService db, PhotoProcessingService photoService, MyDesk.Shared.Services.Extraction.DocumentExtractionService extractionService) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        // Verify expense belongs to user's tenant
        var expenseResult = await db.QueryAsync(
            @"SELECT TenantId FROM dbo.Expenses WHERE ExpenseId = @ExpenseId",
            new() { ["ExpenseId"] = expenseId });

        if (expenseResult.Rows.Count == 0 || (int)expenseResult.Rows[0]["TenantId"] != tenantId)
            return Results.Forbid();

        var form = await ctx.Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault("receipt");

        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "No receipt file provided" });

        using var stream = file.OpenReadStream();
        var (isValid, error) = await photoService.ValidateImageAsync(stream, file.ContentType ?? "", maxSizeBytes: 10485760);

        if (!isValid)
            return Results.BadRequest(new { error });

        // Reset stream position after validation
        stream.Position = 0;

        // Extract receipt data using AI
        var extraction = await extractionService.ProcessAsync(stream, file.ContentType ?? "image/jpeg", file.FileName);

        // Save receipt photo
        stream.Position = 0;
        var filePath = await photoService.SaveImageAsync(stream, tenantId.ToString(), $"receipts/{expenseId}", file.FileName);

        // Store receipt in database
        var insertResult = await db.QueryAsync(
            @"INSERT INTO dbo.ExpenseReceipts (ExpenseId, TenantId, FileName, ContentType, FilePath, FileSizeBytes, ExtractionStrategy, ExtractionConfidence, ExtractionAuditPassed, ExtractedSupplierName, ExtractedDate, ExtractedAmount, ExtractedGst, ExtractedDescription, ExtractedRawText, Status, ExtractionStatus, RequiresManualReview, CreatedBy)
              OUTPUT INSERTED.ReceiptId
              VALUES (@ExpenseId, @TenantId, @FileName, @ContentType, @FilePath, @FileSize, @Strategy, @Confidence, @AuditPassed, @Supplier, @DocDate, @Amount, @Gst, @Description, @RawText, 'Pending', 'Completed', @RequiresReview, @UserId)",
            new()
            {
                ["ExpenseId"] = expenseId,
                ["TenantId"] = tenantId,
                ["FileName"] = file.FileName,
                ["ContentType"] = file.ContentType ?? "image/jpeg",
                ["FilePath"] = filePath,
                ["FileSize"] = file.Length,
                ["Strategy"] = extraction.StrategyUsed,
                ["Confidence"] = extraction.Confidence,
                ["AuditPassed"] = extraction.AuditPassed,
                ["Supplier"] = extraction.SupplierName ?? DBNull.Value,
                ["DocDate"] = extraction.DocumentDate ?? DBNull.Value,
                ["Amount"] = extraction.TotalAmount ?? 0,
                ["Gst"] = extraction.GstAmount ?? 0,
                ["Description"] = extraction.LineItems.Count > 0 ? string.Join(", ", extraction.LineItems.Select(x => x.Description)) : DBNull.Value,
                ["RawText"] = extraction.RawText ?? DBNull.Value,
                ["RequiresReview"] = extraction.Confidence < 0.80 ? 1 : 0,
                ["UserId"] = userId
            });

        int receiptId = (int)insertResult.Rows[0]["ReceiptId"];

        // Log audit
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.ExpenseReceiptAudit (TenantId, ExpenseId, ReceiptId, Action, ConfidenceScore, ExtractionStrategy, ExtractedFields, AuditedBy)
              VALUES (@TenantId, @ExpenseId, @ReceiptId, 'Uploaded', @Confidence, @Strategy, @Fields, @AuditedBy)",
            new()
            {
                ["TenantId"] = tenantId,
                ["ExpenseId"] = expenseId,
                ["ReceiptId"] = receiptId,
                ["Confidence"] = extraction.Confidence,
                ["Strategy"] = extraction.StrategyUsed,
                ["Fields"] = System.Text.Json.JsonSerializer.Serialize(new { extraction.SupplierName, extraction.DocumentDate, extraction.TotalAmount, extraction.GstAmount }),
                ["AuditedBy"] = userId
            });

        return Results.Ok(new
        {
            receiptId,
            filePath,
            extraction = new
            {
                supplierName = extraction.SupplierName,
                date = extraction.DocumentDate,
                totalAmount = extraction.TotalAmount,
                gstAmount = extraction.GstAmount,
                confidence = Math.Round(extraction.Confidence * 100, 2),
                auditPassed = extraction.AuditPassed,
                strategy = extraction.StrategyUsed,
                requiresManualReview = extraction.Confidence < 0.80
            },
            message = "Receipt uploaded and extracted successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UploadExpenseReceipt")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization()
.DisableAntiforgery();
*/

// GET /api/expenses/{expenseId}/receipt - Get receipt data
app.MapGet("/api/expenses/{expenseId:int}/receipt", async (int expenseId, HttpContext ctx, DatabaseService db) =>
{
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (tenantId == 0)
        return Results.Unauthorized();

    try
    {
        // Verify expense belongs to tenant
        var expenseResult = await db.QueryAsync(
            @"SELECT TenantId FROM dbo.Expenses WHERE ExpenseId = @ExpenseId",
            new() { ["ExpenseId"] = expenseId });

        if (expenseResult.Rows.Count == 0 || (int)expenseResult.Rows[0]["TenantId"] != tenantId)
            return Results.Forbid();

        var result = await db.QueryAsync(
            @"SELECT ReceiptId, FileName, FilePath, ExtractionStrategy, ExtractionConfidence, ExtractedSupplierName, ExtractedDate, ExtractedAmount, ExtractedGst, CorrectedSupplierName, CorrectedDate, CorrectedAmount, Status, RequiresManualReview
              FROM dbo.ExpenseReceipts
              WHERE ExpenseId = @ExpenseId AND Status != 'Archived'
              ORDER BY CreatedAt DESC",
            new() { ["ExpenseId"] = expenseId });

        if (result.Rows.Count == 0)
            return Results.NotFound(new { error = "No receipt found for this expense" });

        var receipts = result.Rows.Cast<System.Data.DataRow>().Select(row => new
        {
            receiptId = (int)row["ReceiptId"],
            fileName = (string)row["FileName"],
            filePath = (string)row["FilePath"],
            extractionStrategy = row["ExtractionStrategy"]?.ToString(),
            extractionConfidence = Convert.ToDouble(row["ExtractionConfidence"] ?? 0),
            extracted = new
            {
                supplierName = row["ExtractedSupplierName"]?.ToString(),
                date = row["ExtractedDate"],
                amount = Convert.ToDecimal(row["ExtractedAmount"] ?? 0),
                gst = Convert.ToDecimal(row["ExtractedGst"] ?? 0)
            },
            corrected = new
            {
                supplierName = row["CorrectedSupplierName"]?.ToString(),
                date = row["CorrectedDate"],
                amount = (decimal?)(row["CorrectedAmount"] != DBNull.Value ? Convert.ToDecimal(row["CorrectedAmount"]) : null)
            },
            status = (string)row["Status"],
            requiresManualReview = (bool)row["RequiresManualReview"]
        }).ToList();

        return Results.Ok(new { receipts });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetExpenseReceipt")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// ── Notifications System (Phase 3) ────────────────────────────────────────────

// GET /api/notifications - Get unread in-app notifications
app.MapGet("/api/notifications", async (HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        var result = await db.QueryAsync(
            @"SELECT NotificationId, Title, Message, Icon, ActionUrl, ActionText, Type, Category, EntityType, EntityId, CreatedAt, IsRead
              FROM dbo.InAppNotifications
              WHERE TenantId = @TenantId AND UserId = @UserId AND IsRead = 0 AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE())
              ORDER BY CreatedAt DESC
              OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        var countResult = await db.QueryAsync(
            "SELECT UnreadTotal FROM dbo.NotificationState WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        int unreadCount = countResult.Rows.Count > 0 ? (int)countResult.Rows[0]["UnreadTotal"] : 0;

        var notifications = result.Rows.Cast<System.Data.DataRow>().Select(row => new
        {
            notificationId = (int)row["NotificationId"],
            title = row["Title"]?.ToString(),
            message = row["Message"]?.ToString(),
            icon = row["Icon"]?.ToString(),
            actionUrl = row["ActionUrl"]?.ToString(),
            actionText = row["ActionText"]?.ToString(),
            type = row["Type"]?.ToString(),
            category = row["Category"]?.ToString(),
            entityType = row["EntityType"]?.ToString(),
            entityId = row["EntityId"] != DBNull.Value ? (int?)row["EntityId"] : null,
            createdAt = (DateTime)row["CreatedAt"],
            isRead = (bool)row["IsRead"]
        }).ToList();

        return Results.Ok(new { notifications, unreadCount });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetNotifications")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/notifications/{notificationId}/read - Mark as read
app.MapPost("/api/notifications/{notificationId:int}/read", async (int notificationId, HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;

    if (userId == 0)
        return Results.Unauthorized();

    try
    {
        // Verify ownership
        var verifyResult = await db.QueryAsync(
            "SELECT UserId FROM dbo.InAppNotifications WHERE NotificationId = @NotificationId",
            new() { ["NotificationId"] = notificationId });

        if (verifyResult.Rows.Count == 0 || (int)verifyResult.Rows[0]["UserId"] != userId)
            return Results.Forbid();

        await db.ExecuteNonQueryAsync(
            "UPDATE dbo.InAppNotifications SET IsRead = 1, ReadAt = GETUTCDATE() WHERE NotificationId = @NotificationId",
            new() { ["NotificationId"] = notificationId });

        return Results.Ok(new { message = "Notification marked as read" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("MarkNotificationAsRead")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// POST /api/notifications/read-all - Mark all as read
app.MapPost("/api/notifications/read-all", async (HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.InAppNotifications SET IsRead = 1, ReadAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND UserId = @UserId AND IsRead = 0",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.NotificationState SET UnreadTotal = 0 WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        return Results.Ok(new { message = "All notifications marked as read" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("MarkAllNotificationsAsRead")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// GET /api/notifications/preferences - Get notification preferences
app.MapGet("/api/notifications/preferences", async (HttpContext ctx, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        var result = await db.QueryAsync(
            @"SELECT EnableEmailNotifications, EmailOnApprovalRequired, EmailDigestFrequency,
                     EnableSmsNotifications, PhoneNumber, EnableInAppNotifications,
                     QuietHoursEnabled, QuietHoursStart, QuietHoursEnd
              FROM dbo.NotificationSettings
              WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        if (result.Rows.Count == 0)
            return Results.NotFound();

        var prefs = result.Rows[0];
        return Results.Ok(new
        {
            enableEmailNotifications = (bool)prefs["EnableEmailNotifications"],
            emailOnApprovalRequired = (bool)prefs["EmailOnApprovalRequired"],
            emailDigestFrequency = prefs["EmailDigestFrequency"]?.ToString(),
            enableSmsNotifications = (bool)prefs["EnableSmsNotifications"],
            phoneNumber = prefs["PhoneNumber"]?.ToString(),
            enableInAppNotifications = (bool)prefs["EnableInAppNotifications"],
            quietHoursEnabled = (bool)prefs["QuietHoursEnabled"],
            quietHoursStart = prefs["QuietHoursStart"],
            quietHoursEnd = prefs["QuietHoursEnd"]
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetNotificationPreferences")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// PUT /api/notifications/preferences - Update preferences
app.MapPut("/api/notifications/preferences", async (HttpContext ctx, UpdateNotificationPreferencesRequest body, DatabaseService db) =>
{
    var userId = int.TryParse(ctx.User.FindFirst("UserId")?.Value, out var uid) ? uid : 0;
    var tenantId = int.TryParse(ctx.User.FindFirst("tenant_id")?.Value, out var tid) ? tid : 0;

    if (userId == 0 || tenantId == 0)
        return Results.Unauthorized();

    try
    {
        await db.ExecuteNonQueryAsync(
            @"UPDATE dbo.NotificationSettings
              SET EnableEmailNotifications = @EmailEnabled,
                  EmailOnApprovalRequired = @EmailApproval,
                  EmailDigestFrequency = @DigestFreq,
                  EnableSmsNotifications = @SmsEnabled,
                  PhoneNumber = @Phone,
                  EnableInAppNotifications = @InAppEnabled,
                  QuietHoursEnabled = @QuietEnabled,
                  QuietHoursStart = @QuietStart,
                  QuietHoursEnd = @QuietEnd,
                  ModifiedAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND UserId = @UserId",
            new()
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["EmailEnabled"] = body.EnableEmailNotifications,
                ["EmailApproval"] = body.EmailOnApprovalRequired,
                ["DigestFreq"] = body.EmailDigestFrequency,
                ["SmsEnabled"] = body.EnableSmsNotifications,
                ["Phone"] = body.PhoneNumber ?? (object)DBNull.Value,
                ["InAppEnabled"] = body.EnableInAppNotifications,
                ["QuietEnabled"] = body.QuietHoursEnabled,
                ["QuietStart"] = body.QuietHoursStart ?? (object)DBNull.Value,
                ["QuietEnd"] = body.QuietHoursEnd ?? (object)DBNull.Value
            });

        return Results.Ok(new { message = "Preferences updated" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UpdateNotificationPreferences")
// .WithOpenApi()  // DISABLED - method not available
.RequireAuthorization();

// Phase 4 API endpoints and DTOs temporarily disabled for debugging CI
// TODO: Re-enable after resolving build issues
// All services (DepartmentService, TeamService, BudgetService, ApprovalDelegationService,
// ApprovalEscalationService, BulkUserImportService) and database schema (Migration 022)
// are in place and ready for endpoint activation.

app.Run();

// =============================================================================
// REQUEST/RESPONSE CLASSES (After app.Run() to ensure all top-level statements
// are defined first)
// =============================================================================

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public record MobileLoginRequest(string Login, string Password);

public record DeskyChatRequest(
    string Message,
    string? Brand = null,
    List<DeskyChatMessage>? History = null);

public record DeskyChatMessage(string Role, string Content);

public record EmailRequest(
    string To,
    string? Subject    = null,
    string? Message    = null,
    bool    AttachPdf  = true);

public record PatCreateRequest(
    string  Name,
    string? Scopes     = null,
    int?    ExpiryDays = null);

class AddDomainRequest
{
    public string Domain { get; set; } = "";
}

class VerifyDomainRequest
{
    public string Domain { get; set; } = "";
    public string VerificationToken { get; set; } = "";
}

class CreateApprovalPermissionRequest
{
    public string? RoleId { get; set; }
    public int UserId { get; set; }
    public string ModuleType { get; set; } = "Expense";
    public int ApprovalLevel { get; set; } = 1;
    public decimal? MinThreshold { get; set; }
    public decimal? MaxThreshold { get; set; }
    public bool? CanDelegate { get; set; }
    public bool? CanReject { get; set; }
    public bool? CanComment { get; set; }
}

class UpdateApprovalPermissionRequest
{
    public decimal? MinThreshold { get; set; }
    public decimal? MaxThreshold { get; set; }
    public bool? CanDelegate { get; set; }
    public bool? CanReject { get; set; }
}

class CheckApprovalPermissionRequest
{
    public string ModuleType { get; set; } = "Expense";
    public int ApprovalLevel { get; set; } = 1;
    public decimal? Amount { get; set; }
}

class RecordSecurityEventRequest
{
    public string EventType { get; set; } = "";
    public string Severity { get; set; } = "WARNING";
    public int? UserId { get; set; }
    public string Description { get; set; } = "";
    public int? AffectedRecords { get; set; }
    public string? IpAddress { get; set; }
}

class InvestigateSecurityEventRequest
{
    public string Notes { get; set; } = "";
    public bool IsResolved { get; set; } = false;
}

class UpdateBillingConfigRequest
{
    public string BillingModel { get; set; } = "MONTHLY_ADVANCE";
    public string? BillingContactEmail { get; set; }
    public string? Currency { get; set; }
    public decimal? TaxPercentage { get; set; }
    public int? CycleStartDay { get; set; }
    public int? CycleStartMonth { get; set; }
}

class MarkInvoicePaidRequest
{
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
}

class StartOnboardingRequest
{
    public string AdminName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
}

class OnboardingStepRequest
{
    public string? TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string? Domain { get; set; }
    public string? ApprovalWorkflow { get; set; }
    public string? BillingModel { get; set; }
    public string? BillingContactEmail { get; set; }
    public int UserSeats { get; set; }
}

class UpdateNotificationPreferencesRequest
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EmailOnApprovalRequired { get; set; } = true;
    public string EmailDigestFrequency { get; set; } = "Immediate";
    public bool EnableSmsNotifications { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public bool EnableInAppNotifications { get; set; } = true;
    public bool QuietHoursEnabled { get; set; } = false;
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}

class DelegationRequest
{
    public int DelegateUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ModuleType { get; set; }
}

class DelegateApprovalRequest
{
    public int DelegateUserId { get; set; }
}
