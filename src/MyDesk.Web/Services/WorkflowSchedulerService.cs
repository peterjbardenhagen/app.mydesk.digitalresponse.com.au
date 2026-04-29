using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Background scheduler for automated workflow triggers per Proposal #272:
/// - End-of-Day reconciliation check
/// - End-of-Week reconciliation + email to Bert
/// - End-of-Month MYOB sync
/// - Overdue payment alerts
/// </summary>
public class WorkflowSchedulerService : BackgroundService
{
    private readonly ILogger<WorkflowSchedulerService> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    public WorkflowSchedulerService(
        ILogger<WorkflowSchedulerService> logger,
        IServiceProvider services,
        IConfiguration config)
    {
        _logger = logger;
        _services = services;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue<bool>("Workflows:Enabled", false))
        {
            _logger.LogInformation("Workflow scheduler disabled (set Workflows:Enabled=true to enable)");
            return;
        }

        _logger.LogInformation("Workflow scheduler started");

        // Check every 15 minutes whether any scheduled workflow is due
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckScheduledWorkflowsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled workflow check failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task CheckScheduledWorkflowsAsync()
    {
        var now = DateTime.Now;

        // End-of-day: run at 6:00 PM on weekdays
        if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday
            && now.Hour == 18 && now.Minute < 15)
        {
            await RunEndOfDayAsync();
        }

        // End-of-week: Friday 5:00 PM
        if (now.DayOfWeek == DayOfWeek.Friday && now.Hour == 17 && now.Minute < 15)
        {
            await RunEndOfWeekAsync();
        }

        // End-of-month: last day of month at 4:00 PM
        var lastDay = DateTime.DaysInMonth(now.Year, now.Month);
        if (now.Day == lastDay && now.Hour == 16 && now.Minute < 15)
        {
            await RunEndOfMonthAsync();
        }
    }

    private async Task RunEndOfDayAsync()
    {
        _logger.LogInformation("Running end-of-day workflow");
        using var scope = _services.CreateScope();
        var recon = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();

        try
        {
            var unsynced = await recon.GetUnsyncedInvoicesAsync(DateTime.Today, DateTime.Today.AddDays(1));
            if (unsynced.Count > 0)
            {
                var body = $"End-of-day check found {unsynced.Count} invoice(s) created today not yet in MYOB:\n\n" +
                           string.Join("\n", unsynced.Select(i => $"• {i.InvoiceNum} — {i.CustomerName} — {i.TotalIncGST:C}"));

                var recipient = _config["Workflows:NotificationEmail"];
                var platformName = _config["PlatformSettings:PlatformName"] ?? "MyDesk";
                if (!string.IsNullOrEmpty(recipient))
                {
                    await email.SendAsync(recipient, $"[{platformName}] End-of-Day: Unsynced Invoices", body);
                }
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "End-of-day failed"); }
    }

    private async Task RunEndOfWeekAsync()
    {
        _logger.LogInformation("Running end-of-week workflow");
        using var scope = _services.CreateScope();
        var recon = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();

        try
        {
            var summary = await recon.GetSummaryAsync();
            var aged = await recon.GetAggAgedReceivablesAsync();
            var issues = await recon.RunDataQualityChecksAsync();

            var body = $@"Weekly Reconciliation Report — {DateTime.Now:dd MMM yyyy}

═══════════════════════════════════════
SUMMARY
═══════════════════════════════════════
Unsynced to MYOB:     {summary.UnsyncedCount} invoices ({summary.UnsyncedTotal:C})
Outstanding AR:       {summary.OutstandingCount} invoices ({summary.OutstandingTotal:C})
This month's sales:   {summary.MonthlySalesCount} invoices ({summary.MonthlySalesTotal:C})
Quarterly GST:        {summary.QuarterlyGstTotal:C}

═══════════════════════════════════════
AGED RECEIVABLES
═══════════════════════════════════════
Current (0-30 days):  {aged.Current:C}
31-60 days:           {aged.Days31_60:C}
61-90 days:           {aged.Days61_90:C}
Over 90 days:         {aged.Over90:C}
TOTAL:                {aged.Total:C}

═══════════════════════════════════════
DATA QUALITY ISSUES: {issues.Count}
═══════════════════════════════════════
{(issues.Count == 0 ? "No issues detected." : string.Join("\n", issues.Take(10).Select(i => $"[{i.Severity}] {i.Category}: {i.Description}")))}
";
            var recipient = _config["Workflows:NotificationEmail"];
            var platformName = _config["PlatformSettings:PlatformName"] ?? "MyDesk";
            if (!string.IsNullOrEmpty(recipient))
            {
                await email.SendAsync(recipient, $"[{platformName}] Weekly Reconciliation Report", body);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "End-of-week failed"); }
    }

    private async Task RunEndOfMonthAsync()
    {
        _logger.LogInformation("Running end-of-month workflow");
        using var scope = _services.CreateScope();
        var recon = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();

        try
        {
            var unsynced = await recon.GetUnsyncedInvoicesAsync(
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now);

            var platformName = _config["PlatformSettings:PlatformName"] ?? "MyDesk";
            var body = $@"Month-End Reconciliation Reminder — {DateTime.Now:MMMM yyyy}

There are {unsynced.Count} invoice(s) from this month totalling {unsynced.Sum(i => i.TotalIncGST):C}
that have not yet been pushed to MYOB.

Please complete the month-end sync via the platform reconciliation page.

Top invoices pending sync:
{string.Join("\n", unsynced.Take(20).Select(i => $"• {i.InvoiceNum} — {i.CustomerName} — {i.TotalIncGST:C}"))}
";
            var recipient = _config["Workflows:NotificationEmail"];
            if (!string.IsNullOrEmpty(recipient))
            {
                await email.SendAsync(recipient, $"[{platformName}] Month-End MYOB Sync Required", body);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "End-of-month failed"); }
    }
}
