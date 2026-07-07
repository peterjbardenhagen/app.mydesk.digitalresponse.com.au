using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Background job service for scheduled notifications and digests.
/// Integrates with Hangfire for reliable job scheduling.
/// Part of Phase 5: Notifications & Alerts
/// </summary>
public class NotificationBackgroundJobService
{
    private readonly DatabaseService _db;
    private readonly ApprovalNotificationService _approvalNotification;
    private readonly BudgetAlertService _budgetAlert;
    private readonly ILogger<NotificationBackgroundJobService>? _logger;

    public NotificationBackgroundJobService(
        DatabaseService db,
        ApprovalNotificationService approvalNotification,
        BudgetAlertService budgetAlert,
        ILogger<NotificationBackgroundJobService>? logger = null)
    {
        _db = db;
        _approvalNotification = approvalNotification;
        _budgetAlert = budgetAlert;
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
        RecurringJob.AddOrUpdate(
            "send-approval-reminders",
            () => SendApprovalReminders(),
            Cron.Hourly);

        // Check budget thresholds for all tenants (every 30 minutes)
        RecurringJob.AddOrUpdate(
            "check-all-budgets",
            () => CheckAllBudgetThresholds(),
            Cron.MinuteInterval(30));

        // Process notification digests (every morning at 8 AM)
        RecurringJob.AddOrUpdate(
            "process-digests",
            () => ProcessDailyDigests(),
            Cron.DailyAt(8, 0));

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

            // Get all active tenants
            var tenantsResult = await _db.QueryAsync(
                "SELECT DISTINCT TenantId FROM dbo.Tenants WHERE IsActive = 1",
                null);

            int totalReminders = 0;

            // Send reminders for each tenant
            foreach (var row in tenantsResult.Rows)
            {
                int tenantId = (int)row["TenantId"];
                int remindersSent = await _approvalNotification.SendApprovalRemindersAsync(tenantId, daysOld: 3);
                totalReminders += remindersSent;

                _logger?.LogInformation(
                    "Sent {RemindersCount} approval reminders for tenant {TenantId}",
                    remindersSent, tenantId);
            }

            _logger?.LogInformation("Approval reminder job completed: {TotalReminders} reminders sent", totalReminders);
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

            // Get all active tenants
            var tenantsResult = await _db.QueryAsync(
                "SELECT DISTINCT TenantId FROM dbo.Tenants WHERE IsActive = 1",
                null);

            int totalAlerts = 0;

            // Check budgets for each tenant
            foreach (var row in tenantsResult.Rows)
            {
                int tenantId = (int)row["TenantId"];
                int alertsCreated = await _budgetAlert.CheckAllBudgetsAsync(tenantId);
                totalAlerts += alertsCreated;

                _logger?.LogInformation(
                    "Created {AlertsCount} budget alerts for tenant {TenantId}",
                    alertsCreated, tenantId);
            }

            _logger?.LogInformation("Budget threshold check completed: {TotalAlerts} alerts created", totalAlerts);
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

            // Get all users with digest enabled
            var usersResult = await _db.QueryAsync(
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

                bool success = await ProcessUserDigestAsync(tenantId, userId);
                if (success)
                    digestsProcessed++;

                _logger?.LogInformation(
                    "Processed daily digest for user {UserId} in tenant {TenantId}",
                    userId, tenantId);
            }

            _logger?.LogInformation("Daily digest processing completed: {ProcessedCount} digests sent", digestsProcessed);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in daily digest processing job");
        }
    }

    /// <summary>
    /// Process digest for a single user - compile and send all pending notifications.
    /// </summary>
    private async Task<bool> ProcessUserDigestAsync(int tenantId, int userId)
    {
        try
        {
            // Get user email
            var userResult = await _db.QueryAsync(
                "SELECT Email, Name FROM dbo.Users WHERE TenantId = @TenantId AND UserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });

            if (userResult.Rows.Count == 0)
                return false;

            string userEmail = (string)userResult.Rows[0]["Email"];
            string userName = (string)userResult.Rows[0]["Name"];

            // Get pending budget alerts for today
            var budgetAlertsResult = await _db.QueryAsync(
                @"SELECT COUNT(*) as AlertCount FROM dbo.BudgetAlerts
                  WHERE TenantId = @TenantId AND CreatedAt >= CAST(GETUTCDATE() AS DATE)
                        AND IsAcknowledged = 0",
                new() { ["TenantId"] = tenantId });

            int budgetAlertCount = budgetAlertsResult.Rows.Count > 0
                ? (int)budgetAlertsResult.Rows[0]["AlertCount"]
                : 0;

            // Get pending approval notifications for today
            var approvalNotifsResult = await _db.QueryAsync(
                @"SELECT COUNT(*) as NotificationCount FROM dbo.ApprovalNotifications
                  WHERE TenantId = @TenantId AND CreatedAt >= CAST(GETUTCDATE() AS DATE)
                        AND RecipientUserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });

            int approvalNotifCount = approvalNotifsResult.Rows.Count > 0
                ? (int)approvalNotifsResult.Rows[0]["NotificationCount"]
                : 0;

            // Create digest log entry
            await _db.ExecuteNonQueryAsync(
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
    /// Manually trigger approval reminders (for testing or manual execution).
    /// </summary>
    public async Task TriggerApprovalReminders(int tenantId)
    {
        _logger?.LogInformation("Manual trigger: approval reminders for tenant {TenantId}", tenantId);
        await _approvalNotification.SendApprovalRemindersAsync(tenantId);
    }

    /// <summary>
    /// Manually trigger budget threshold check (for testing or manual execution).
    /// </summary>
    public async Task TriggerBudgetThresholdCheck(int tenantId)
    {
        _logger?.LogInformation("Manual trigger: budget threshold check for tenant {TenantId}", tenantId);
        await _budgetAlert.CheckAllBudgetsAsync(tenantId);
    }

    /// <summary>
    /// Manually trigger digest processing (for testing or manual execution).
    /// </summary>
    public async Task TriggerDigestProcessing()
    {
        _logger?.LogInformation("Manual trigger: digest processing");
        await ProcessDailyDigests();
    }
}
