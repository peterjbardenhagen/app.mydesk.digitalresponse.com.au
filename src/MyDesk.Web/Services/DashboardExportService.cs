using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for exporting dashboard data in multiple formats (CSV, JSON, PDF).
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class DashboardExportService
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<DashboardExportService>? _logger;

    public DashboardExportService(
        AnalyticsService analyticsService,
        ILogger<DashboardExportService>? logger = null)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Export executive dashboard as CSV
    /// </summary>
    public async Task<byte[]> ExportExecutiveDashboardAsCSVAsync(
        int tenantId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting executive dashboard as CSV for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);
        var csv = new StringBuilder();

        if (includeSummary)
        {
            csv.AppendLine("EXECUTIVE DASHBOARD - SUMMARY METRICS");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Expenses MTD,{dashboard.TotalExpensesMonthToDate:C}");
            csv.AppendLine($"Pending Approvals,{dashboard.PendingApprovals}");
            csv.AppendLine($"Approved This Month,{dashboard.ApprovedThisMonth}");
            csv.AppendLine($"Average Approval Time,{dashboard.AverageApprovalTimeHours:F1} hours");
            csv.AppendLine();
        }

        if (includeDetailed)
        {
            csv.AppendLine("EXPENSES BY DEPARTMENT");
            csv.AppendLine("Department,Amount,Count");
            foreach (var dept in dashboard.ByDepartment)
            {
                csv.AppendLine($"{EscapeCSVField(dept.Department)},{dept.Amount:F2},{dept.Count}");
            }
            csv.AppendLine();

            csv.AppendLine("EXPENSES BY CATEGORY");
            csv.AppendLine("Category,Amount,Count");
            foreach (var cat in dashboard.ByCategory)
            {
                csv.AppendLine($"{EscapeCSVField(cat.Category)},{cat.Amount:F2},{cat.Count}");
            }
            csv.AppendLine();

            csv.AppendLine("BUDGET VS ACTUAL");
            csv.AppendLine("Department,Allocated,Spent,Available,Utilization %");
            foreach (var budget in dashboard.BudgetVsActual)
            {
                var available = budget.Allocated - budget.Spent;
                var utilization = budget.Allocated > 0 ? (budget.Spent / budget.Allocated * 100) : 0;
                csv.AppendLine(
                    $"{EscapeCSVField(budget.Department)},{budget.Allocated:F2},{budget.Spent:F2},{available:F2},{utilization:F1}");
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Export manager dashboard as CSV
    /// </summary>
    public async Task<byte[]> ExportManagerDashboardAsCSVAsync(
        int tenantId, int managerId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting manager dashboard as CSV for manager {ManagerId} in tenant {TenantId}",
            managerId, tenantId);

        var dashboard = await _analyticsService.GetManagerDashboardAsync(tenantId, managerId);
        var csv = new StringBuilder();

        if (includeSummary)
        {
            csv.AppendLine("MANAGER DASHBOARD - TEAM SUMMARY");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Team,{EscapeCSVField(dashboard.TeamName)}");
            csv.AppendLine($"Team Members,{dashboard.TeamMembersCount}");
            csv.AppendLine($"Expenses MTD,{dashboard.TeamExpensesMonthToDate:C}");
            csv.AppendLine($"Pending Approvals,{dashboard.PendingApprovals}");
            csv.AppendLine($"Overdue Approvals,{dashboard.OverdueApprovals}");
            csv.AppendLine();
        }

        if (includeDetailed)
        {
            csv.AppendLine("TEAM SPENDING BY CATEGORY");
            csv.AppendLine("Category,Amount");
            foreach (var cat in dashboard.TeamSpendingByCategory)
            {
                csv.AppendLine($"{EscapeCSVField(cat.Category)},{cat.Amount:F2}");
            }
            csv.AppendLine();

            if (dashboard.OverdueItems?.Count > 0)
            {
                csv.AppendLine("OVERDUE ITEMS");
                csv.AppendLine("Employee,Amount,Days Overdue");
                foreach (var item in dashboard.OverdueItems.Take(100))
                {
                    csv.AppendLine(
                        $"{EscapeCSVField(item.EmployeeName)},{item.Amount:F2},{item.DaysOverdue}");
                }
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Export employee dashboard as CSV
    /// </summary>
    public async Task<byte[]> ExportEmployeeDashboardAsCSVAsync(
        int tenantId, int userId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting employee dashboard as CSV for user {UserId} in tenant {TenantId}",
            userId, tenantId);

        var dashboard = await _analyticsService.GetEmployeeDashboardAsync(tenantId, userId);
        var csv = new StringBuilder();

        if (includeSummary)
        {
            csv.AppendLine("EMPLOYEE DASHBOARD - PERSONAL SUMMARY");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Employee,{EscapeCSVField(dashboard.EmployeeName)}");
            csv.AppendLine($"Submitted This Month,{dashboard.SubmittedThisMonth}");
            csv.AppendLine($"Approved This Month,{dashboard.ApprovedThisMonth}");
            csv.AppendLine($"Pending Approval,{dashboard.PendingApproval}");
            csv.AppendLine($"Reimbursed This Month,{dashboard.ReimbursedThisMonth}");
            csv.AppendLine();
        }

        if (includeDetailed && dashboard.MyExpenses?.Count > 0)
        {
            csv.AppendLine("MY EXPENSES");
            csv.AppendLine("ID,Description,Amount,Category,Status");
            foreach (var expense in dashboard.MyExpenses.Take(100))
            {
                csv.AppendLine(
                    $"{expense.Id},{EscapeCSVField(expense.Description)},{expense.Amount:F2},{EscapeCSVField(expense.Category)},{EscapeCSVField(expense.Status)}");
            }
            csv.AppendLine();

            if (dashboard.MonthlySummary?.Count > 0)
            {
                csv.AppendLine("MONTHLY SUMMARY");
                csv.AppendLine("Month,Amount");
                foreach (var month in dashboard.MonthlySummary)
                {
                    csv.AppendLine($"{EscapeCSVField(month.Month)},{month.Amount:F2}");
                }
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Export executive dashboard as JSON
    /// </summary>
    public async Task<byte[]> ExportExecutiveDashboardAsJsonAsync(
        int tenantId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting executive dashboard as JSON for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);

        var exportData = new
        {
            metadata = new
            {
                exportedAt = DateTime.UtcNow,
                dashboardType = "Executive",
                tenantId = tenantId
            },
            summary = includeSummary ? new
            {
                totalExpensesMonthToDate = dashboard.TotalExpensesMonthToDate,
                pendingApprovals = dashboard.PendingApprovals,
                approvedThisMonth = dashboard.ApprovedThisMonth,
                averageApprovalTimeHours = dashboard.AverageApprovalTimeHours
            } : null,
            detailed = includeDetailed ? new
            {
                byDepartment = dashboard.ByDepartment?.Select(d => new
                {
                    department = d.Department,
                    amount = d.Amount,
                    count = d.Count
                }).ToList(),
                byCategory = dashboard.ByCategory?.Select(c => new
                {
                    category = c.Category,
                    amount = c.Amount,
                    count = c.Count
                }).ToList(),
                budgetVsActual = dashboard.BudgetVsActual?.Select(b => new
                {
                    department = b.Department,
                    allocated = b.Allocated,
                    spent = b.Spent,
                    available = b.Allocated - b.Spent,
                    utilizationPercent = b.Allocated > 0 ? Math.Round(b.Spent / b.Allocated * 100, 2) : 0
                }).ToList()
            } : null
        };

        var json = JsonSerializer.Serialize(
            exportData,
            new JsonSerializerOptions { WriteIndented = true });

        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Export manager dashboard as JSON
    /// </summary>
    public async Task<byte[]> ExportManagerDashboardAsJsonAsync(
        int tenantId, int managerId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting manager dashboard as JSON for manager {ManagerId} in tenant {TenantId}",
            managerId, tenantId);

        var dashboard = await _analyticsService.GetManagerDashboardAsync(tenantId, managerId);

        var exportData = new
        {
            metadata = new
            {
                exportedAt = DateTime.UtcNow,
                dashboardType = "Manager",
                tenantId = tenantId,
                managerId = managerId
            },
            summary = includeSummary ? new
            {
                teamName = dashboard.TeamName,
                teamMembersCount = dashboard.TeamMembersCount,
                teamExpensesMonthToDate = dashboard.TeamExpensesMonthToDate,
                pendingApprovals = dashboard.PendingApprovals,
                overdueApprovals = dashboard.OverdueApprovals
            } : null,
            detailed = includeDetailed ? new
            {
                teamSpendingByCategory = dashboard.TeamSpendingByCategory?.Select(c => new
                {
                    category = c.Category,
                    amount = c.Amount
                }).ToList(),
                overdueItems = dashboard.OverdueItems?.Take(100).Select(e => new
                {
                    employeeName = e.EmployeeName,
                    amount = e.Amount,
                    daysOverdue = e.DaysOverdue
                }).ToList()
            } : null
        };

        var json = JsonSerializer.Serialize(
            exportData,
            new JsonSerializerOptions { WriteIndented = true });

        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Export employee dashboard as JSON
    /// </summary>
    public async Task<byte[]> ExportEmployeeDashboardAsJsonAsync(
        int tenantId, int userId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting employee dashboard as JSON for user {UserId} in tenant {TenantId}",
            userId, tenantId);

        var dashboard = await _analyticsService.GetEmployeeDashboardAsync(tenantId, userId);

        var exportData = new
        {
            metadata = new
            {
                exportedAt = DateTime.UtcNow,
                dashboardType = "Employee",
                tenantId = tenantId,
                userId = userId
            },
            summary = includeSummary ? new
            {
                employeeName = dashboard.EmployeeName,
                submittedThisMonth = dashboard.SubmittedThisMonth,
                approvedThisMonth = dashboard.ApprovedThisMonth,
                pendingApproval = dashboard.PendingApproval,
                reimbursedThisMonth = dashboard.ReimbursedThisMonth
            } : null,
            detailed = includeDetailed ? new
            {
                myExpenses = dashboard.MyExpenses?.Take(100).Select(e => new
                {
                    id = e.Id,
                    description = e.Description,
                    amount = e.Amount,
                    category = e.Category,
                    status = e.Status
                }).ToList(),
                monthlySummary = dashboard.MonthlySummary?.Select(m => new
                {
                    month = m.Month,
                    amount = m.Amount
                }).ToList()
            } : null
        };

        var json = JsonSerializer.Serialize(
            exportData,
            new JsonSerializerOptions { WriteIndented = true });

        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Escape CSV field to handle commas, quotes, and newlines
    /// </summary>
    private static string EscapeCSVField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
