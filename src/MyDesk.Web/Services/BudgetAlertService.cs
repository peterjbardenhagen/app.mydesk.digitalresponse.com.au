using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MyDesk.Shared.Services;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Budget Alert Service: Monitor departmental spending and alert when thresholds exceeded
/// Part of Phase 5: Notifications & Alerts (extension of Phase 4 Budget Management)
/// </summary>
public class BudgetAlertService
{
    private readonly DatabaseService _db;
    private readonly NotificationService _notification;
    private readonly BudgetService _budget;
    private readonly ILogger<BudgetAlertService>? _logger;

    // Default threshold percentages
    private const int ThresholdWarning = 80;
    private const int ThresholdCritical = 100;

    public BudgetAlertService(
        DatabaseService db,
        NotificationService notification,
        BudgetService budget,
        ILogger<BudgetAlertService>? logger = null)
    {
        _db = db;
        _notification = notification;
        _budget = budget;
        _logger = logger;
    }

    /// <summary>
    /// Check budget threshold and send alert if exceeded
    /// Called after expense operations to monitor spending
    /// </summary>
    public async Task<bool> CheckBudgetThresholdAsync(int tenantId, int departmentId)
    {
        try
        {
            _logger?.LogInformation("Checking budget threshold for department {DepartmentId}", departmentId);

            // Get current budget for this fiscal year
            var budgetYear = DateTime.UtcNow.Year;
            var budgetResult = await _db.QueryAsync(
                @"SELECT BudgetId, AllocatedAmount, SpentAmount, EncumberedAmount,
                         ThresholdAlertPercentage, AllowOverspend
                  FROM DepartmentBudgets
                  WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId AND FiscalYear = @Year",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["DepartmentId"] = departmentId,
                    ["Year"] = budgetYear
                });

            if (budgetResult.Rows.Count == 0)
                return false;  // No budget configured

            var budget = budgetResult.Rows[0];
            decimal allocated = (decimal)budget["AllocatedAmount"];
            decimal spent = (decimal)budget["SpentAmount"];
            decimal encumbered = (decimal)budget["EncumberedAmount"];
            int thresholdPercent = budget["ThresholdAlertPercentage"] != DBNull.Value
                ? (int)budget["ThresholdAlertPercentage"]
                : ThresholdWarning;
            bool allowOverspend = (bool)budget["AllowOverspend"];
            int budgetId = (int)budget["BudgetId"];

            decimal totalCommitted = spent + encumbered;
            double usagePercentage = allocated > 0 ? (totalCommitted / allocated) * 100 : 0;

            _logger?.LogInformation(
                "Budget usage: {Usage}% ({Spent} of {Allocated})",
                usagePercentage, totalCommitted, allocated);

            // Determine alert type
            string? alertType = null;
            string? alertLevel = null;

            if (usagePercentage >= 100)
            {
                alertType = "Full";
                alertLevel = "Critical";
            }
            else if (usagePercentage >= thresholdPercent)
            {
                alertType = "Threshold";
                alertLevel = "Warning";
            }
            else if (totalCommitted > allocated && !allowOverspend)
            {
                alertType = "Overspend";
                alertLevel = "Critical";
            }

            if (alertType == null)
                return false;  // No alert needed

            // Check if we already sent this alert recently (within 24 hours)
            var recentAlert = await _db.QueryAsync(
                @"SELECT TOP 1 AlertId FROM BudgetAlerts
                  WHERE TenantId = @TenantId AND BudgetId = @BudgetId
                        AND AlertType = @AlertType
                        AND CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())
                  ORDER BY CreatedAt DESC",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["BudgetId"] = budgetId,
                    ["AlertType"] = alertType
                });

            if (recentAlert.Rows.Count > 0)
            {
                _logger?.LogInformation("Alert already sent in last 24 hours, skipping");
                return false;
            }

            // Create alert record
            int alertId = await CreateBudgetAlertAsync(
                tenantId, departmentId, budgetId,
                (int)usagePercentage, spent, allocated, alertType, alertLevel);

            // Send notifications to department managers
            await NotifyDepartmentManagersAsync(tenantId, departmentId, budgetId, alertType, usagePercentage, spent, allocated);

            _logger?.LogInformation("Budget alert {AlertId} created and notifications sent", alertId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking budget threshold");
            return false;
        }
    }

    /// <summary>
    /// Create budget alert record in database
    /// </summary>
    private async Task<int> CreateBudgetAlertAsync(
        int tenantId, int departmentId, int budgetId,
        int usagePercentage, decimal spent, decimal allocated,
        string alertType, string alertLevel)
    {
        var result = await _db.QueryAsync(
            @"INSERT INTO BudgetAlerts (TenantId, DepartmentId, BudgetId, UsagePercentage, SpentAmount, AllocatedAmount, AlertType, AlertLevel, CreatedAt)
              OUTPUT INSERTED.AlertId
              VALUES (@TenantId, @DepartmentId, @BudgetId, @Usage, @Spent, @Allocated, @Type, @Level, GETUTCDATE())",
            new()
            {
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId,
                ["BudgetId"] = budgetId,
                ["Usage"] = usagePercentage,
                ["Spent"] = spent,
                ["Allocated"] = allocated,
                ["Type"] = alertType,
                ["Level"] = alertLevel
            });

        return result.Rows.Count > 0 ? (int)result.Rows[0]["AlertId"] : 0;
    }

    /// <summary>
    /// Send notifications to department managers about budget alert
    /// </summary>
    private async Task NotifyDepartmentManagersAsync(
        int tenantId, int departmentId, int budgetId,
        string alertType, double usagePercentage, decimal spent, decimal allocated)
    {
        // Get department name
        var deptResult = await _db.QueryAsync(
            "SELECT Name, ManagerUserId FROM Departments WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId",
            new() { ["TenantId"] = tenantId, ["DepartmentId"] = departmentId });

        if (deptResult.Rows.Count == 0)
            return;

        string departmentName = (string)deptResult.Rows[0]["Name"];
        int? managerUserId = deptResult.Rows[0]["ManagerUserId"] as int?;

        // Get team leads in department
        var leadsResult = await _db.QueryAsync(
            @"SELECT DISTINCT TeamLeadUserId FROM Teams
              WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId AND TeamLeadUserId IS NOT NULL",
            new() { ["TenantId"] = tenantId, ["DepartmentId"] = departmentId });

        // Prepare notification content
        var placeholders = new Dictionary<string, object>
        {
            { "DepartmentName", departmentName },
            { "UsagePercentage", Math.Round(usagePercentage, 1) },
            { "SpentAmount", spent.ToString("C") },
            { "AllocatedAmount", allocated.ToString("C") },
            { "RemainingAmount", (allocated - spent).ToString("C") },
            { "AlertType", alertType }
        };

        // Send to department manager
        if (managerUserId.HasValue && managerUserId > 0)
        {
            await _notification.SendNotificationAsync(
                tenantId,
                managerUserId.Value,
                "BudgetAlert",
                placeholders,
                "Department",
                departmentId);

            _logger?.LogInformation("Sent budget alert to manager {UserId}", managerUserId);
        }

        // Send to team leads
        foreach (DataRow leadRow in leadsResult.Rows)
        {
            if (leadRow["TeamLeadUserId"] is int leadId && leadId > 0)
            {
                await _notification.SendNotificationAsync(
                    tenantId,
                    leadId,
                    "BudgetAlert",
                    placeholders,
                    "Department",
                    departmentId);

                _logger?.LogInformation("Sent budget alert to team lead {UserId}", leadId);
            }
        }
    }

    /// <summary>
    /// Get budget alert history for a department
    /// </summary>
    public async Task<DataTable> GetBudgetAlertHistoryAsync(int tenantId, int departmentId, int days = 30)
    {
        var result = await _db.QueryAsync(
            @"SELECT AlertId, UsagePercentage, SpentAmount, AllocatedAmount, AlertType, AlertLevel, IsAcknowledged, CreatedAt
              FROM BudgetAlerts
              WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId
                    AND CreatedAt >= DATEADD(DAY, -@Days, GETUTCDATE())
              ORDER BY CreatedAt DESC",
            new()
            {
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId,
                ["Days"] = days
            });

        return result;
    }

    /// <summary>
    /// Acknowledge a budget alert (mark as read by manager)
    /// </summary>
    public async Task<bool> AcknowledgeBudgetAlertAsync(int tenantId, int alertId, int acknowledgedByUserId)
    {
        try
        {
            int affected = await _db.ExecuteAsync(
                @"UPDATE BudgetAlerts
                  SET IsAcknowledged = 1, AcknowledgedAt = GETUTCDATE(), AcknowledgedBy = @UserId
                  WHERE TenantId = @TenantId AND AlertId = @AlertId",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["AlertId"] = alertId,
                    ["UserId"] = acknowledgedByUserId
                });

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return false;
        }
    }

    /// <summary>
    /// Get unacknowledged alerts for a user's departments
    /// </summary>
    public async Task<DataTable> GetUnacknowledgedAlertsAsync(int tenantId, int userId)
    {
        var result = await _db.QueryAsync(
            @"SELECT BA.AlertId, BA.DepartmentId, D.Name AS DepartmentName,
                     BA.UsagePercentage, BA.AlertType, BA.AlertLevel, BA.CreatedAt
              FROM BudgetAlerts BA
              JOIN Departments D ON BA.DepartmentId = D.DepartmentId
              WHERE BA.TenantId = @TenantId
                    AND BA.IsAcknowledged = 0
                    AND (D.ManagerUserId = @UserId OR EXISTS (
                        SELECT 1 FROM Teams T WHERE T.TenantId = @TenantId AND T.DepartmentId = D.DepartmentId AND T.TeamLeadUserId = @UserId
                    ))
              ORDER BY BA.CreatedAt DESC",
            new()
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId
            });

        return result;
    }

    /// <summary>
    /// Check all departmental budgets (for scheduled job)
    /// </summary>
    public async Task<int> CheckAllBudgetsAsync(int tenantId)
    {
        try
        {
            _logger?.LogInformation("Running budget threshold check for all departments in tenant {TenantId}", tenantId);

            // Get all active budgets for current fiscal year
            var budgetsResult = await _db.QueryAsync(
                @"SELECT DISTINCT DepartmentId FROM DepartmentBudgets
                  WHERE TenantId = @TenantId AND FiscalYear = YEAR(GETUTCDATE()) AND Status = 'Active'",
                new() { ["TenantId"] = tenantId });

            int alertsCreated = 0;
            foreach (DataRow row in budgetsResult.Rows)
            {
                int deptId = (int)row["DepartmentId"];
                if (await CheckBudgetThresholdAsync(tenantId, deptId))
                    alertsCreated++;
            }

            _logger?.LogInformation("Created {Count} budget alerts", alertsCreated);
            return alertsCreated;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking all budgets");
            return 0;
        }
    }
}
