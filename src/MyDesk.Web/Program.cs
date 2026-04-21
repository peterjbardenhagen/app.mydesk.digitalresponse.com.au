using MudBlazor.Services;
using MudBlazor;
using MyDesk.Shared.Services;
using MyDesk.Web.Components;
using MyDesk.Web.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
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
builder.Host.UseSerilog();

// Authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
});

// Database (singleton - wraps connection string, opens new conn per query)
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<NavMenuService>();
builder.Services.AddSingleton<SetupMenuService>();
builder.Services.AddSingleton<EntityColorService>();
builder.Services.AddSingleton<UserPreferencesService>();

// Platform settings service (tenant-aware)
builder.Services.AddSingleton<PlatformSettingsService>();

// Auth service (scoped to request)
builder.Services.AddScoped<AuthService>();

// Domain services (all in MyDesk.Shared.Services)
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddSingleton<ITargetsProvider, TargetsProvider>();
builder.Services.AddScoped<IntelligenceService>();
builder.Services.Configure<MarketingOptions>(builder.Configuration.GetSection("Marketing"));
builder.Services.AddSingleton<MarketingService>();
builder.Services.AddScoped<MarketingDataService>();
builder.Services.AddScoped<MarketingAIService>();
builder.Services.AddSingleton<MarketingStrategyStore>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SystemService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DespatchService>();
builder.Services.AddScoped<JobOrderService>();
builder.Services.AddScoped<NoticeboardService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<TransporterService>();

builder.Services.AddHttpClient();
builder.Services.Configure<AzureAIOptions>(builder.Configuration.GetSection(AzureAIOptions.Section));
builder.Services.AddScoped<AzureAIService>();
builder.Services.AddScoped<McpIntegrationService>();

// Proposal #272: AI Enhancement services
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<AiAuditService>();
builder.Services.AddScoped<TelegramBotService>();
builder.Services.AddHostedService<WorkflowSchedulerService>();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Set QuestPDF community license (free tier, required since v2023.12)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Ensure audit + email log tables exist (idempotent IF NOT EXISTS)
// Wrapped in try/catch so app still starts even if database is temporarily unavailable
using (var scope = app.Services.CreateScope())
{
    try
    {
        var actSvc   = scope.ServiceProvider.GetRequiredService<ActivityService>();
        await actSvc.EnsureTableAsync();
        var emailSvc = scope.ServiceProvider.GetRequiredService<EmailService>();
        await emailSvc.EnsureTablesAsync();
        var aiAudit  = scope.ServiceProvider.GetRequiredService<AiAuditService>();
        await aiAudit.EnsureTableAsync();
        var reportSvc = scope.ServiceProvider.GetRequiredService<ReportService>();
        await reportSvc.EnsureTableAsync();
        Log.Information("Database tables verified successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization failed - app will start but database features may be unavailable. Check connection string in appsettings.json");
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
        }
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "text/html";
        await ctx.Response.WriteAsync(app.Environment.IsDevelopment()
            ? $"<h1>500 - Server Error</h1><pre>{System.Net.WebUtility.HtmlEncode(feature?.Error?.ToString() ?? "Unknown")}</pre>"
            : "<h1>500 - Server Error</h1><p>An unexpected error occurred. Check the server logs for details.</p>");
    });
});

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login/logout endpoints
app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var login = form["login"].ToString();
    var password = form["password"].ToString();

    var user = await auth.ValidateLoginAsync(login, password);
    if (user != null)
    {
        Log.Information("Login SUCCESS: {Login} -> UserId={UserId} Code={Code} Name={Name}",
            login, user.UserId, user.Code, user.Name);
        await auth.SignInAsync(ctx, user);
        return Results.Redirect("/");
    }
    Log.Warning("Login FAILED for {Login} from {RemoteIP}",
        login, ctx.Connection.RemoteIpAddress);
    return Results.Redirect("/login?error=1");
});

// Forgot password endpoint
app.MapPost("/api/auth/forgot-password", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();

    Log.Information("Password reset requested for {Email} from {RemoteIP}",
        email, ctx.Connection.RemoteIpAddress);

    // TODO: Implement actual password reset email sending
    // For now, redirect to success state to avoid 404
    return Results.Redirect("/forgot-password?success=1");
});

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

app.MapGet("/logout", async (HttpContext ctx) =>
{
    Log.Information("Logout: {User}", ctx.User?.Identity?.Name ?? "anonymous");
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

Log.Information("Application configured. Environment={Env}, URLs={Urls}",
    app.Environment.EnvironmentName, string.Join(", ", app.Urls.DefaultIfEmpty("default")));

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
