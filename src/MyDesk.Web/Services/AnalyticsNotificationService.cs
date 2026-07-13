using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for sending analytics-related notifications to users.
/// Alerts users about detected anomalies, budget concerns, and important insights.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class AnalyticsNotificationService
{
    private readonly AnomalyDetectionService _anomalyService;
    private readonly NotificationService _notificationService;
    private readonly DatabaseService _db;
    private readonly ILogger<AnalyticsNotificationService>? _logger;

    public AnalyticsNotificationService(
        AnomalyDetectionService anomalyService,
        NotificationService notificationService,
        DatabaseService db,
        ILogger<AnalyticsNotificationService>? logger = null)
    {
        _anomalyService = anomalyService;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Run analytics notifications check for a tenant
    /// </summary>
    public async Task CheckAndNotifyAsync(int tenantId)
    {
        _logger?.LogInformation("Running analytics notification check for tenant {TenantId}", tenantId);

        try
        {
            // Detect anomalies
            var expenseAnomalies = await _anomalyService.DetectExpenseAnomaliesAsync(tenantId);
            var budgetAnomalies = await _anomalyService.DetectBudgetAnomaliesAsync(tenantId);

            // Notify relevant users
            if (expenseAnomalies.Count > 0)
            {
                await NotifyExpenseAnomaliesAsync(tenantId, expenseAnomalies);
            }

            if (budgetAnomalies.Count > 0)
            {
                await NotifyBudgetAnomaliesAsync(tenantId, budgetAnomalies);
            }

            _logger?.LogInformation(
                "Analytics notification check complete for tenant {TenantId}: {ExpenseCount} expense, {BudgetCount} budget anomalies",
                tenantId, expenseAnomalies.Count, budgetAnomalies.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error running analytics notification check for tenant {TenantId}", tenantId);
        }
    }

    private async Task NotifyExpenseAnomaliesAsync(int tenantId, List<ExpenseAnomaly> anomalies)
    {
        // Group by user
        var anomaliesByUser = anomalies
            .Where(a => a.UserId.HasValue)
            .GroupBy(a => a.UserId)
            .ToList();

        foreach (var userGroup in anomaliesByUser)
        {
            var userId = userGroup.Key;
            var userAnomalies = userGroup.ToList();

            // Get user details
            var userDt = await _db.QueryAsync(
                "SELECT UserId, Email, Name FROM Users WHERE UserId = @UserId",
                new() { ["UserId"] = userId });

            if (userDt.Rows.Count > 0)
            {
                var row = userDt.Rows[0];
                var email = row["Email"].ToString() ?? "";
                var userName = row["Name"].ToString() ?? "";

                // Group anomalies by severity for summary
                var criticalCount = userAnomalies.Count(a => a.Severity == AnomalySeverity.Critical);
                var highCount = userAnomalies.Count(a => a.Severity == AnomalySeverity.High);

                var eventType = criticalCount > 0 ? "expense_anomaly_critical" : "expense_anomaly_detected";

                _logger?.LogInformation(
                    "Notifying user {UserId} ({Email}) of {Count} expense anomalies",
                    userId, email, userAnomalies.Count);

                await _notificationService.SendNotificationAsync(
                    tenantId,
                    userId,
                    eventType,
                    new Dictionary<string, object>
                    {
                        ["count"] = userAnomalies.Count,
                        ["critical_count"] = criticalCount,
                        ["high_count"] = highCount
                    });
            }
        }

        // Notify finance/management if high severity anomalies
        var criticalAnomalies = anomalies.Where(a => a.Severity == AnomalySeverity.Critical).ToList();
        if (criticalAnomalies.Count > 0)
        {
            await NotifyFinanceTeamAsync(tenantId, "Expense Anomalies", criticalAnomalies);
        }
    }

    private async Task NotifyBudgetAnomaliesAsync(int tenantId, List<BudgetAnomaly> anomalies)
    {
        // Get finance/management users for the tenant
        var financeUsers = await _db.QueryAsync(
            @"SELECT DISTINCT u.UserId, u.Email, u.Name
              FROM Users u
              WHERE u.TenantId = @TenantId AND u.Role IN ('Admin', 'Finance', 'Manager')
              ORDER BY u.Role DESC",
            new() { ["TenantId"] = tenantId });

        var criticalBudgets = anomalies.Where(a => a.Severity >= AnomalySeverity.High).ToList();

        if (criticalBudgets.Count > 0)
        {
            foreach (System.Data.DataRow row in financeUsers.Rows)
            {
                var userId = (int)row["UserId"];
                var email = row["Email"].ToString() ?? "";
                var userName = row["Name"].ToString() ?? "";

                _logger?.LogInformation(
                    "Notifying finance user {UserId} ({Email}) of {Count} budget anomalies",
                    userId, email, criticalBudgets.Count);

                await _notificationService.SendNotificationAsync(
                    tenantId,
                    userId,
                    "budget_anomaly_detected",
                    new Dictionary<string, object>
                    {
                        ["count"] = criticalBudgets.Count,
                        ["over_budget"] = criticalBudgets.Count(a => a.PercentUsed >= 100)
                    });
            }
        }
    }

    private async Task NotifyFinanceTeamAsync(int tenantId, string category, dynamic anomalies)
    {
        var financeUsers = await _db.QueryAsync(
            @"SELECT u.UserId, u.Email, u.Name
              FROM Users u
              WHERE u.TenantId = @TenantId AND u.Role IN ('Admin', 'Finance')
              ORDER BY u.Role",
            new() { ["TenantId"] = tenantId });

        foreach (System.Data.DataRow row in financeUsers.Rows)
        {
            var userId = (int)row["UserId"];

            await _notificationService.SendNotificationAsync(
                tenantId,
                userId,
                "critical_anomaly_detected",
                new Dictionary<string, object>
                {
                    ["category"] = category,
                    ["count"] = ((List<dynamic>)anomalies).Count
                });
        }
    }

}
