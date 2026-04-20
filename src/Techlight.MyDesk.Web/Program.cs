using MudBlazor.Services;
using MudBlazor;
using Techlight.MyDesk.Shared.Services;
using Techlight.MyDesk.Web.Components;
using Techlight.MyDesk.Web.Services;
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
Log.Information("Techlight MyDesk starting up - PID {Pid}, Logs at {LogsDir}", Environment.ProcessId, logsDir);
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

// Auth service (scoped to request)
builder.Services.AddScoped<AuthService>();

// Domain services (all in Techlight.MyDesk.Shared.Services)
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SystemService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DespatchService>();
builder.Services.AddScoped<JobOrderService>();
builder.Services.AddScoped<NoticeboardService>();

builder.Services.AddHttpClient();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Set QuestPDF community license (free tier, required since v2023.12)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Ensure audit + email log tables exist (idempotent IF NOT EXISTS)
using (var scope = app.Services.CreateScope())
{
    var actSvc   = scope.ServiceProvider.GetRequiredService<ActivityService>();
    await actSvc.EnsureTableAsync();
    var emailSvc = scope.ServiceProvider.GetRequiredService<EmailService>();
    await emailSvc.EnsureTablesAsync();
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
