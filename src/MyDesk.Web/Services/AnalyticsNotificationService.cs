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

                var subject = criticalCount > 0
                    ? $"⚠️ Critical Expense Alert - {criticalCount} anomalies detected"
                    : $"📊 Expense Anomaly Alert - {userAnomalies.Count} issues detected";

                var body = BuildExpenseAnomalyNotification(userName, userAnomalies);

                _logger?.LogInformation(
                    "Notifying user {UserId} ({Email}) of {Count} expense anomalies",
                    userId, email, userAnomalies.Count);

                await _notificationService.SendNotificationAsync(
                    userId,
                    tenantId,
                    subject,
                    body,
                    NotificationType.Alert);
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

                var subject = $"🚨 Budget Alert - {criticalBudgets.Count} budgets require attention";
                var body = BuildBudgetAnomalyNotification(userName, criticalBudgets);

                _logger?.LogInformation(
                    "Notifying finance user {UserId} ({Email}) of {Count} budget anomalies",
                    userId, email, criticalBudgets.Count);

                await _notificationService.SendNotificationAsync(
                    userId,
                    tenantId,
                    subject,
                    body,
                    NotificationType.Alert);
            }
        }
    }

    private async Task NotifyFinanceTeamAsync(int tenantId, string category, dynamic anomalies)
    {
        var financeUsers = await _db.QueryAsync(
            @"SELECT u.UserId, u.Email, u.Name
              FROM Users u
              WHERE u.TenantId = @TenantId AND u.Role IN ('Admin', 'Finance')
              LIMIT 5",
            new() { ["TenantId"] = tenantId });

        foreach (System.Data.DataRow row in financeUsers.Rows)
        {
            var userId = (int)row["UserId"];
            var subject = $"Finance Alert: {category}";
            var body = $"<p>Critical {category.ToLower()} detected requiring immediate attention.</p>";

            await _notificationService.SendNotificationAsync(
                userId,
                tenantId,
                subject,
                body,
                NotificationType.Alert);
        }
    }

    private string BuildExpenseAnomalyNotification(string userName, List<ExpenseAnomaly> anomalies)
    {
        var body = $@"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#333;'>
            <p>Hi {userName},</p>
            <p>We've detected {anomalies.Count} unusual expense pattern(s) in your recent submissions:</p>
            <ul style='list-style:none;padding:0;'>";

        foreach (var anomaly in anomalies.OrderByDescending(a => a.Severity))
        {
            var severityColor = anomaly.Severity switch
            {
                AnomalySeverity.Critical => "#d32f2f",
                AnomalySeverity.High => "#f57c00",
                AnomalySeverity.Medium => "#fbc02d",
                _ => "#388e3c"
            };

            body += $@"
                <li style='margin-bottom:12px;padding:12px;background:#f5f5f5;border-left:4px solid {severityColor};'>
                    <strong>{anomaly.AnomalyType}</strong> ({anomaly.Severity})
                    <br/>{anomaly.Description}
                    <br/><small style='color:#666;'>Amount: ${anomaly.Amount:F2} | {anomaly.ExpenseDate:MMM d, yyyy}</small>
                </li>";
        }

        body += @"
            </ul>
            <p>Please review these expenses and contact your manager if you believe these alerts are incorrect.</p>
            <p style='color:#666;font-size:12px;margin-top:20px;'>This is an automated notification. Do not reply to this email.</p>
        </div>";

        return body;
    }

    private string BuildBudgetAnomalyNotification(string userName, List<BudgetAnomaly> anomalies)
    {
        var body = $@"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#333;'>
            <p>Hi {userName},</p>
            <p>Budget alerts for your review:</p>
            <table style='width:100%;border-collapse:collapse;'>
                <tr style='background:#f5f5f5;border-bottom:1px solid #ddd;'>
                    <th style='padding:10px;text-align:left;'>Category</th>
                    <th style='padding:10px;text-align:right;'>Budget</th>
                    <th style='padding:10px;text-align:right;'>Spent</th>
                    <th style='padding:10px;text-align:right;'>Used</th>
                    <th style='padding:10px;text-align:center;'>Status</th>
                </tr>";

        foreach (var anomaly in anomalies.OrderByDescending(a => a.PercentUsed))
        {
            var status = anomaly.PercentUsed >= 100 ? "❌ Over Budget" : "⚠️ High Usage";
            body += $@"
                <tr style='border-bottom:1px solid #ddd;'>
                    <td style='padding:10px;'>{anomaly.Category}</td>
                    <td style='padding:10px;text-align:right;'>${anomaly.BudgetAmount:F2}</td>
                    <td style='padding:10px;text-align:right;'>${anomaly.SpentAmount:F2}</td>
                    <td style='padding:10px;text-align:right;'>{anomaly.PercentUsed}%</td>
                    <td style='padding:10px;text-align:center;'>{status}</td>
                </tr>";
        }

        body += @"
            </table>
            <p style='color:#666;font-size:12px;margin-top:20px;'>This is an automated notification. Do not reply to this email.</p>
        </div>";

        return body;
    }
}
