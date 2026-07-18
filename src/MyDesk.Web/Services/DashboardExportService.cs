using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    /// Export executive dashboard as PDF
    /// </summary>
    public async Task<byte[]> ExportExecutiveDashboardAsPdfAsync(
        int tenantId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting executive dashboard as PDF for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("EXECUTIVE DASHBOARD")
                        .FontSize(24)
                        .Bold()
                        .FontColor("#2c3e50");

                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10)
                        .FontColor("#7f8c8d");

                    if (includeSummary)
                    {
                        column.Item().PaddingTop(10).PaddingBottom(5).Text("Summary Metrics").FontSize(14).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Metric").Bold();
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Value").Bold();
                            });

                            table.Cell().Text("Total Expenses MTD");
                            table.Cell().Text(dashboard.TotalExpensesMonthToDate.ToString("C"));

                            table.Cell().Text("Pending Approvals");
                            table.Cell().Text(dashboard.PendingApprovals.ToString());

                            table.Cell().Text("Approved This Month");
                            table.Cell().Text(dashboard.ApprovedThisMonth.ToString());

                            table.Cell().Text("Average Approval Time");
                            table.Cell().Text($"{dashboard.AverageApprovalTimeHours:F1}h");
                        });
                    }

                    if (includeDetailed)
                    {
                        column.Item().PaddingTop(15).PaddingBottom(5).Text("Expenses by Department").FontSize(14).Bold();

                        if (dashboard.ByDepartment?.Count > 0)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Department").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Count").Bold();
                                });

                                foreach (var dept in dashboard.ByDepartment)
                                {
                                    table.Cell().Text(dept.Department);
                                    table.Cell().Text(dept.Amount.ToString("C"));
                                    table.Cell().Text(dept.Count.ToString());
                                }
                            });
                        }

                        column.Item().PaddingTop(15).PaddingBottom(5).Text("Expenses by Category").FontSize(14).Bold();

                        if (dashboard.ByCategory?.Count > 0)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Category").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Count").Bold();
                                });

                                foreach (var cat in dashboard.ByCategory)
                                {
                                    table.Cell().Text(cat.Category);
                                    table.Cell().Text(cat.Amount.ToString("C"));
                                    table.Cell().Text(cat.Count.ToString());
                                }
                            });
                        }

                        column.Item().PaddingTop(15).PaddingBottom(5).Text("Budget vs Actual").FontSize(14).Bold();

                        if (dashboard.BudgetVsActual?.Count > 0)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Department").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Allocated").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Spent").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Available").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("% Used").Bold();
                                });

                                foreach (var budget in dashboard.BudgetVsActual)
                                {
                                    var available = budget.Allocated - budget.Spent;
                                    var utilization = budget.Allocated > 0 ? (budget.Spent / budget.Allocated * 100) : 0;

                                    table.Cell().Text(budget.Department);
                                    table.Cell().Text(budget.Allocated.ToString("C"));
                                    table.Cell().Text(budget.Spent.ToString("C"));
                                    table.Cell().Text(available.ToString("C"));
                                    table.Cell().Text($"{utilization:F1}%");
                                }
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontColor("#7f8c8d");
                    x.CurrentPageNumber().FontColor("#7f8c8d");
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Export manager dashboard as PDF
    /// </summary>
    public async Task<byte[]> ExportManagerDashboardAsPdfAsync(
        int tenantId, int managerId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting manager dashboard as PDF for manager {ManagerId} in tenant {TenantId}",
            managerId, tenantId);

        var dashboard = await _analyticsService.GetManagerDashboardAsync(tenantId, managerId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("MANAGER DASHBOARD")
                        .FontSize(24)
                        .Bold()
                        .FontColor("#2c3e50");

                    column.Item().Text($"Team: {dashboard.TeamName}")
                        .FontSize(12);

                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10)
                        .FontColor("#7f8c8d");

                    if (includeSummary)
                    {
                        column.Item().PaddingTop(10).PaddingBottom(5).Text("Team Summary").FontSize(14).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Metric").Bold();
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Value").Bold();
                            });

                            table.Cell().Text("Team Members");
                            table.Cell().Text(dashboard.TeamMembersCount.ToString());

                            table.Cell().Text("Expenses MTD");
                            table.Cell().Text(dashboard.TeamExpensesMonthToDate.ToString("C"));

                            table.Cell().Text("Pending Approvals");
                            table.Cell().Text(dashboard.PendingApprovals.ToString());

                            table.Cell().Text("Overdue Approvals");
                            table.Cell().Text(dashboard.OverdueApprovals.ToString());
                        });
                    }

                    if (includeDetailed)
                    {
                        column.Item().PaddingTop(15).PaddingBottom(5).Text("Team Spending by Category").FontSize(14).Bold();

                        if (dashboard.TeamSpendingByCategory?.Count > 0)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Category").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                });

                                foreach (var cat in dashboard.TeamSpendingByCategory)
                                {
                                    table.Cell().Text(cat.Category);
                                    table.Cell().Text(cat.Amount.ToString("C"));
                                }
                            });
                        }

                        if (dashboard.OverdueItems?.Count > 0)
                        {
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("Overdue Approvals").FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Employee").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Days Overdue").Bold();
                                });

                                foreach (var item in dashboard.OverdueItems.Take(50))
                                {
                                    table.Cell().Text(item.EmployeeName);
                                    table.Cell().Text(item.Amount.ToString("C"));
                                    table.Cell().Text(item.DaysOverdue.ToString());
                                }
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontColor("#7f8c8d");
                    x.CurrentPageNumber().FontColor("#7f8c8d");
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Export employee dashboard as PDF
    /// </summary>
    public async Task<byte[]> ExportEmployeeDashboardAsPdfAsync(
        int tenantId, int userId, bool includeCharts = true, bool includeDetailed = true, bool includeSummary = true)
    {
        _logger?.LogInformation("Exporting employee dashboard as PDF for user {UserId} in tenant {TenantId}",
            userId, tenantId);

        var dashboard = await _analyticsService.GetEmployeeDashboardAsync(tenantId, userId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("MY DASHBOARD")
                        .FontSize(24)
                        .Bold()
                        .FontColor("#2c3e50");

                    column.Item().Text($"Employee: {dashboard.EmployeeName}")
                        .FontSize(12);

                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10)
                        .FontColor("#7f8c8d");

                    if (includeSummary)
                    {
                        column.Item().PaddingTop(10).PaddingBottom(5).Text("Expense Summary").FontSize(14).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Metric").Bold();
                                header.Cell().Background("#ecf0f1").Padding(5).Text("Value").Bold();
                            });

                            table.Cell().Text("Submitted This Month");
                            table.Cell().Text(dashboard.SubmittedThisMonth.ToString());

                            table.Cell().Text("Approved");
                            table.Cell().Text(dashboard.ApprovedThisMonth.ToString());

                            table.Cell().Text("Pending Approval");
                            table.Cell().Text(dashboard.PendingApproval.ToString());

                            table.Cell().Text("Reimbursed");
                            table.Cell().Text(dashboard.ReimbursedThisMonth.ToString());
                        });
                    }

                    if (includeDetailed)
                    {
                        if (dashboard.MyExpenses?.Count > 0)
                        {
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("Recent Expenses").FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Description").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Category").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Status").Bold();
                                });

                                foreach (var expense in dashboard.MyExpenses.Take(50))
                                {
                                    table.Cell().Text(expense.Description);
                                    table.Cell().Text(expense.Amount.ToString("C"));
                                    table.Cell().Text(expense.Category);
                                    table.Cell().Text(expense.Status);
                                }
                            });
                        }

                        if (dashboard.MonthlySummary?.Count > 0)
                        {
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("Monthly Summary").FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Month").Bold();
                                    header.Cell().Background("#ecf0f1").Padding(5).Text("Amount").Bold();
                                });

                                foreach (var month in dashboard.MonthlySummary)
                                {
                                    table.Cell().Text(month.Month);
                                    table.Cell().Text(month.Amount.ToString("C"));
                                }
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontColor("#7f8c8d");
                    x.CurrentPageNumber().FontColor("#7f8c8d");
                });
            });
        });

        return document.GeneratePdf();
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
