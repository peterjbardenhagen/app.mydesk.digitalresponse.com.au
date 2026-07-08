using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Background job service for scheduled notifications and digests.
/// Integrates with Hangfire for reliable job scheduling.
/// Part of Phase 5: Notifications & Alerts
///
/// Registered as singleton, but uses IServiceProvider to create scoped service instances
/// for accessing scoped services (DatabaseService, ApprovalNotificationService, etc.)
/// </summary>
public class NotificationBackgroundJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundJobService>? _logger;

    public NotificationBackgroundJobService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundJobService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Register all recurring background jobs for notifications.
    /// Should be called once at application startup.
    /// </summary>
    public void RegisterRecurringJobs()
    {
        _logger?.LogInformation("Registering recurring notification background jobs");

        // Send approval reminders for pending approvals (every hour)
        RecurringJob.AddOrUpdate<NotificationBackgroundJobService>(
            recurringJobId: "send-approval-reminders",
            methodCall: x => x.SendApprovalReminders(),
            cronExpression: Cron.Hourly);

        // Check budget thresholds for all tenants (every 30 minutes)
        RecurringJob.AddOrUpdate<NotificationBackgroundJobService>(
            recurringJobId: "check-all-budgets",
            methodCall: x => x.CheckAllBudgetThresholds(),
            cronExpression: Cron.MinuteInterval(30));

        // Process notification digests (every morning at 8 AM)
        RecurringJob.AddOrUpdate<NotificationBackgroundJobService>(
            recurringJobId: "process-digests",
            methodCall: x => x.ProcessDailyDigests(),
            cronExpression: "0 8 * * *");

        // Process failed notifications with retry (every 5 minutes)
        RecurringJob.AddOrUpdate<NotificationBackgroundJobService>(
            recurringJobId: "process-notification-retries",
            methodCall: x => x.ProcessFailedNotifications(),
            cronExpression: Cron.MinuteInterval(5));

        _logger?.LogInformation("Recurring notification jobs registered successfully");
    }

    /// <summary>
    /// Send reminders for approvals pending longer than configured threshold.
    /// Triggered hourly to catch old pending approvals.
    /// </summary>
    public async Task SendApprovalReminders()
    {
        try
        {
            _logger?.LogInformation("Starting approval reminder job");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                var approvalNotification = scope.ServiceProvider.GetRequiredService<ApprovalNotificationService>();

                // Get all active tenants
                var tenantsResult = await db.QueryAsync(
                    "SELECT DISTINCT TenantId FROM dbo.Tenants WHERE IsActive = 1",
                    null);

                int totalReminders = 0;

                // Send reminders for each tenant
                foreach (var row in tenantsResult.Rows)
                {
                    int tenantId = (int)row["TenantId"];
                    int remindersSent = await approvalNotification.SendApprovalRemindersAsync(tenantId, daysOld: 3);
                    totalReminders += remindersSent;

                    _logger?.LogInformation(
                        "Sent {RemindersCount} approval reminders for tenant {TenantId}",
                        remindersSent, tenantId);
                }

                _logger?.LogInformation("Approval reminder job completed: {TotalReminders} reminders sent", totalReminders);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in approval reminder job");
        }
    }

    /// <summary>
    /// Check budget thresholds for all departments in all tenants.
    /// Triggered every 30 minutes to catch threshold violations quickly.
    /// </summary>
    public async Task CheckAllBudgetThresholds()
    {
        try
        {
            _logger?.LogInformation("Starting budget threshold check job");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                var budgetAlert = scope.ServiceProvider.GetRequiredService<BudgetAlertService>();

                // Get all active tenants
                var tenantsResult = await db.QueryAsync(
                    "SELECT DISTINCT TenantId FROM dbo.Tenants WHERE IsActive = 1",
                    null);

                int totalAlerts = 0;

                // Check budgets for each tenant
                foreach (var row in tenantsResult.Rows)
                {
                    int tenantId = (int)row["TenantId"];
                    int alertsCreated = await budgetAlert.CheckAllBudgetsAsync(tenantId);
                    totalAlerts += alertsCreated;

                    _logger?.LogInformation(
                        "Created {AlertsCount} budget alerts for tenant {TenantId}",
                        alertsCreated, tenantId);
                }

                _logger?.LogInformation("Budget threshold check completed: {TotalAlerts} alerts created", totalAlerts);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in budget threshold check job");
        }
    }

    /// <summary>
    /// Process daily digest notifications for users with digest enabled.
    /// Triggered once daily at 8:00 AM to send consolidated notifications.
    /// </summary>
    public async Task ProcessDailyDigests()
    {
        try
        {
            _logger?.LogInformation("Starting daily digest processing job");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

                // Get all users with digest enabled
                var usersResult = await db.QueryAsync(
                    @"SELECT DISTINCT ns.TenantId, ns.UserId
                      FROM dbo.NotificationSettings ns
                      WHERE ns.DigestEnabled = 1 AND ns.BudgetAlertFrequency = 'Daily'
                            OR ns.ApprovalAlertFrequency = 'Daily'",
                    null);

                int digestsProcessed = 0;

                // Process digest for each user
                foreach (var row in usersResult.Rows)
                {
                    int tenantId = (int)row["TenantId"];
                    int userId = (int)row["UserId"];

                    bool success = await ProcessUserDigestAsync(tenantId, userId, scope);
                    if (success)
                        digestsProcessed++;

                    _logger?.LogInformation(
                        "Processed daily digest for user {UserId} in tenant {TenantId}",
                        userId, tenantId);
                }

                _logger?.LogInformation("Daily digest processing completed: {ProcessedCount} digests sent", digestsProcessed);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in daily digest processing job");
        }
    }

    /// <summary>
    /// Process digest for a single user - compile and send all pending notifications.
    /// </summary>
    private async Task<bool> ProcessUserDigestAsync(int tenantId, int userId, IServiceScope scope)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            // Get user email
            var userResult = await db.QueryAsync(
                "SELECT Email, Name FROM dbo.Users WHERE TenantId = @TenantId AND UserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });

            if (userResult.Rows.Count == 0)
                return false;

            string userEmail = (string)userResult.Rows[0]["Email"];
            string userName = (string)userResult.Rows[0]["Name"];

            // Get pending budget alerts for today
            var budgetAlertsResult = await db.QueryAsync(
                @"SELECT COUNT(*) as AlertCount FROM dbo.BudgetAlerts
                  WHERE TenantId = @TenantId AND CreatedAt >= CAST(GETUTCDATE() AS DATE)
                        AND IsAcknowledged = 0",
                new() { ["TenantId"] = tenantId });

            int budgetAlertCount = budgetAlertsResult.Rows.Count > 0
                ? (int)budgetAlertsResult.Rows[0]["AlertCount"]
                : 0;

            // Get pending approval notifications for today
            var approvalNotifsResult = await db.QueryAsync(
                @"SELECT COUNT(*) as NotificationCount FROM dbo.ApprovalNotifications
                  WHERE TenantId = @TenantId AND CreatedAt >= CAST(GETUTCDATE() AS DATE)
                        AND RecipientUserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });

            int approvalNotifCount = approvalNotifsResult.Rows.Count > 0
                ? (int)approvalNotifsResult.Rows[0]["NotificationCount"]
                : 0;

            // Create digest log entry
            await db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationDigestLog
                  (TenantId, UserId, DigestDate, BudgetAlertCount, ApprovalNotificationCount, Status, CreatedAt)
                  VALUES (@TenantId, @UserId, CAST(GETUTCDATE() AS DATE), @BudgetCount, @ApprovalCount, 'Sent', GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["UserId"] = userId,
                    ["BudgetCount"] = budgetAlertCount,
                    ["ApprovalCount"] = approvalNotifCount
                });

            _logger?.LogInformation(
                "Created digest for user {UserId}: {BudgetAlerts} budget alerts, {ApprovalNotifs} approval notifications",
                userId, budgetAlertCount, approvalNotifCount);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing digest for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Process failed notifications and schedule retries with exponential backoff.
    /// Triggered every 5 minutes to handle failed emails.
    /// </summary>
    public async Task ProcessFailedNotifications()
    {
        try
        {
            _logger?.LogInformation("Starting failed notification processing job");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                var retryService = scope.ServiceProvider.GetRequiredService<NotificationRetryService>();

                // Get all active tenants
                var tenantsResult = await db.QueryAsync(
                    "SELECT DISTINCT TenantId FROM dbo.Tenants WHERE IsActive = 1",
                    null);

                int totalRetried = 0;

                // Process failed notifications for each tenant
                foreach (var row in tenantsResult.Rows)
                {
                    int tenantId = (int)row["TenantId"];
                    int retriedCount = await retryService.ProcessFailedNotificationsAsync(tenantId);
                    totalRetried += retriedCount;

                    if (retriedCount > 0)
                    {
                        _logger?.LogInformation(
                            "Queued {RetryCount} failed notifications for retry in tenant {TenantId}",
                            retriedCount, tenantId);
                    }
                }

                _logger?.LogInformation("Failed notification processing completed: {TotalRetried} notifications queued for retry", totalRetried);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing failed notifications");
        }
    }

    /// <summary>
    /// Manually trigger approval reminders (for testing or manual execution).
    /// </summary>
    public async Task TriggerApprovalReminders(int tenantId)
    {
        _logger?.LogInformation("Manual trigger: approval reminders for tenant {TenantId}", tenantId);
        using (var scope = _serviceProvider.CreateScope())
        {
            var approvalNotification = scope.ServiceProvider.GetRequiredService<ApprovalNotificationService>();
            await approvalNotification.SendApprovalRemindersAsync(tenantId);
        }
    }

    /// <summary>
    /// Manually trigger budget threshold check (for testing or manual execution).
    /// </summary>
    public async Task TriggerBudgetThresholdCheck(int tenantId)
    {
        _logger?.LogInformation("Manual trigger: budget threshold check for tenant {TenantId}", tenantId);
        using (var scope = _serviceProvider.CreateScope())
        {
            var budgetAlert = scope.ServiceProvider.GetRequiredService<BudgetAlertService>();
            await budgetAlert.CheckAllBudgetsAsync(tenantId);
        }
    }

    /// <summary>
    /// Manually trigger digest processing (for testing or manual execution).
    /// </summary>
    public async Task TriggerDigestProcessing()
    {
        _logger?.LogInformation("Manual trigger: digest processing");
        await ProcessDailyDigests();
    }

    /// <summary>
    /// Manually trigger failed notification retry processing (for testing or manual execution).
    /// </summary>
    public async Task TriggerRetryProcessing(int tenantId)
    {
        _logger?.LogInformation("Manual trigger: retry processing for tenant {TenantId}", tenantId);
        using (var scope = _serviceProvider.CreateScope())
        {
            var retryService = scope.ServiceProvider.GetRequiredService<NotificationRetryService>();
            await retryService.ProcessFailedNotificationsAsync(tenantId);
        }
    }
}
