using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for preparing chart data from dashboard analytics.
/// Provides formatted data for MudBlazor Chart components.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class DashboardChartService
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<DashboardChartService>? _logger;

    public DashboardChartService(
        AnalyticsService analyticsService,
        ILogger<DashboardChartService>? logger = null)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get chart data for expenses by department (pie chart)
    /// </summary>
    public async Task<ChartDataResult> GetDepartmentChartDataAsync(int tenantId)
    {
        _logger?.LogInformation("Preparing department distribution chart for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);

        var chartData = new ChartDataResult
        {
            Labels = dashboard.ByDepartment.Select(d => d.Department).ToList(),
            Series = new List<List<double>>
            {
                dashboard.ByDepartment.Select(d => (double)d.Amount).ToList()
            },
            Colors = GetColorPalette(dashboard.ByDepartment.Count)
        };

        return chartData;
    }

    /// <summary>
    /// Get chart data for expenses by category (pie chart)
    /// </summary>
    public async Task<ChartDataResult> GetCategoryChartDataAsync(int tenantId)
    {
        _logger?.LogInformation("Preparing category distribution chart for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);

        var chartData = new ChartDataResult
        {
            Labels = dashboard.ByCategory.Select(c => c.Category).ToList(),
            Series = new List<List<double>>
            {
                dashboard.ByCategory.Select(c => (double)c.Amount).ToList()
            },
            Colors = GetColorPalette(dashboard.ByCategory.Count)
        };

        return chartData;
    }

    /// <summary>
    /// Get chart data for budget vs actual (bar chart)
    /// </summary>
    public async Task<BarChartDataResult> GetBudgetVsActualChartDataAsync(int tenantId)
    {
        _logger?.LogInformation("Preparing budget vs actual chart for tenant {TenantId}", tenantId);

        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);

        var labels = dashboard.BudgetVsActual.Select(b => b.Department).ToList();
        var allocated = dashboard.BudgetVsActual.Select(b => (double)b.Allocated).ToList();
        var spent = dashboard.BudgetVsActual.Select(b => (double)b.Spent).ToList();

        var chartData = new BarChartDataResult
        {
            Labels = labels,
            Series = new List<DashboardChartSeriesData>
            {
                new()
                {
                    Name = "Allocated",
                    Data = allocated,
                    Color = "#3f51b5"  // Primary blue
                },
                new()
                {
                    Name = "Spent",
                    Data = spent,
                    Color = "#ff9800"  // Warning orange
                }
            }
        };

        return chartData;
    }

    /// <summary>
    /// Get chart data for team spending by category (bar chart)
    /// </summary>
    public async Task<BarChartDataResult> GetTeamSpendingChartDataAsync(int tenantId, int managerId)
    {
        _logger?.LogInformation("Preparing team spending chart for manager {ManagerId} in tenant {TenantId}",
            managerId, tenantId);

        var dashboard = await _analyticsService.GetManagerDashboardAsync(tenantId, managerId);

        var labels = dashboard.TeamSpendingByCategory.Select(c => c.Category).ToList();
        var amounts = dashboard.TeamSpendingByCategory.Select(c => (double)c.Amount).ToList();

        var chartData = new BarChartDataResult
        {
            Labels = labels,
            Series = new List<DashboardChartSeriesData>
            {
                new()
                {
                    Name = "Amount",
                    Data = amounts,
                    Color = "#4caf50"  // Success green
                }
            }
        };

        return chartData;
    }

    /// <summary>
    /// Get chart data for employee monthly spending trend (line chart)
    /// </summary>
    public async Task<LineChartDataResult> GetMonthlyTrendChartDataAsync(int tenantId, int userId)
    {
        _logger?.LogInformation("Preparing monthly trend chart for user {UserId} in tenant {TenantId}",
            userId, tenantId);

        var dashboard = await _analyticsService.GetEmployeeDashboardAsync(tenantId, userId);

        var labels = dashboard.MonthlySummary.Select(m => m.Month).ToList();
        var amounts = dashboard.MonthlySummary.Select(m => (double)m.Amount).ToList();

        var chartData = new LineChartDataResult
        {
            Labels = labels,
            Series = new List<DashboardChartSeriesData>
            {
                new()
                {
                    Name = "Monthly Spending",
                    Data = amounts,
                    Color = "#2196f3"  // Info blue
                }
            }
        };

        return chartData;
    }

    /// <summary>
    /// Get a color palette for charts (supports up to 12 colors)
    /// </summary>
    private static List<string> GetColorPalette(int count)
    {
        var palette = new[]
        {
            "#3f51b5",  // Primary blue
            "#ff9800",  // Warning orange
            "#4caf50",  // Success green
            "#f44336",  // Error red
            "#9c27b0",  // Purple
            "#00bcd4",  // Cyan
            "#ffeb3b",  // Amber
            "#795548",  // Brown
            "#607d8b",  // Blue grey
            "#e91e63",  // Pink
            "#8bc34a",  // Light green
            "#ff5722"   // Deep orange
        };

        return palette.Take(count).ToList();
    }
}

/// <summary>
/// Result for pie/donut charts
/// </summary>
public class ChartDataResult
{
    public List<string> Labels { get; set; } = new();
    public List<List<double>> Series { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}

/// <summary>
/// Result for bar charts
/// </summary>
public class BarChartDataResult
{
    public List<string> Labels { get; set; } = new();
    public List<DashboardChartSeriesData> Series { get; set; } = new();
}

/// <summary>
/// Result for line charts
/// </summary>
public class LineChartDataResult
{
    public List<string> Labels { get; set; } = new();
    public List<DashboardChartSeriesData> Series { get; set; } = new();
}

/// <summary>
/// Individual chart series data
/// </summary>
public class DashboardChartSeriesData
{
    public string Name { get; set; }
    public List<double> Data { get; set; } = new();
    public string Color { get; set; }
}
