using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for detecting anomalies in expense and analytics data.
/// Identifies unusual patterns and trends for proactive notification.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class AnomalyDetectionService
{
    private readonly AnalyticsService _analyticsService;
    private readonly DatabaseService _db;
    private readonly ILogger<AnomalyDetectionService>? _logger;

    public AnomalyDetectionService(
        AnalyticsService analyticsService,
        DatabaseService db,
        ILogger<AnomalyDetectionService>? logger = null)
    {
        _analyticsService = analyticsService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Detect anomalies in expense data
    /// </summary>
    public async Task<List<ExpenseAnomaly>> DetectExpenseAnomaliesAsync(int tenantId, int daysToAnalyze = 30)
    {
        _logger?.LogInformation(
            "Detecting expense anomalies for tenant {TenantId} over {Days} days",
            tenantId, daysToAnalyze);

        var anomalies = new List<ExpenseAnomaly>();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToAnalyze);

            // Get recent expense data
            var expenseData = await _db.QueryAsync(
                @"SELECT TOP 1000
                    e.ExpenseId, e.UserId, e.Amount, e.Category, e.CreatedAt,
                    u.Name AS UserName, u.Email
                  FROM Expenses e
                  JOIN Users u ON u.UserId = e.UserId
                  WHERE e.TenantId = @TenantId AND e.CreatedAt >= @CutoffDate
                  ORDER BY e.CreatedAt DESC",
                new() { ["TenantId"] = tenantId, ["CutoffDate"] = cutoffDate });

            // Check for unusually large expenses
            var amounts = new List<decimal>();
            foreach (System.Data.DataRow row in expenseData.Rows)
            {
                amounts.Add((decimal)row["Amount"]);
            }

            if (amounts.Count > 5)
            {
                var mean = amounts.Average();
                var stdDev = CalculateStandardDeviation(amounts);
                var threshold = mean + (2 * stdDev);

                foreach (System.Data.DataRow row in expenseData.Rows)
                {
                    var amount = (decimal)row["Amount"];
                    if (amount > threshold)
                    {
                        anomalies.Add(new ExpenseAnomaly
                        {
                            AnomalyType = AnomalyType.UnusuallyHighAmount,
                            Severity = CalculateSeverity(amount, threshold),
                            ExpenseId = (int)row["ExpenseId"],
                            UserId = (int)row["UserId"],
                            UserName = row["UserName"].ToString() ?? "",
                            UserEmail = row["Email"].ToString() ?? "",
                            Category = row["Category"].ToString() ?? "",
                            Amount = amount,
                            Description = $"Expense amount ${amount:F2} is significantly higher than typical (threshold: ${threshold:F2})",
                            DetectedAt = DateTime.UtcNow,
                            ExpenseDate = (DateTime)row["CreatedAt"]
                        });
                    }
                }
            }

            // Check for unusual spending patterns per user
            anomalies.AddRange(await DetectUnusualUserSpendingAsync(tenantId, expenseData));

            // Check for category concentration
            anomalies.AddRange(await DetectCategoryConcentrationAsync(tenantId, expenseData));

            _logger?.LogInformation("Detected {Count} anomalies for tenant {TenantId}", anomalies.Count, tenantId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error detecting expense anomalies for tenant {TenantId}", tenantId);
        }

        return anomalies;
    }

    /// <summary>
    /// Detect budget overruns and projection issues
    /// </summary>
    public async Task<List<BudgetAnomaly>> DetectBudgetAnomaliesAsync(int tenantId)
    {
        _logger?.LogInformation("Detecting budget anomalies for tenant {TenantId}", tenantId);

        var anomalies = new List<BudgetAnomaly>();

        try
        {
            var currentMonth = DateTime.UtcNow;
            var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);

            var budgetData = await _db.QueryAsync(
                @"SELECT
                    b.BudgetId, b.DepartmentId, b.Category, b.BudgetAmount, b.AlertThreshold,
                    d.DepartmentName,
                    COALESCE(SUM(e.Amount), 0) AS SpentAmount
                  FROM Budgets b
                  LEFT JOIN Departments d ON d.DepartmentId = b.DepartmentId
                  LEFT JOIN Expenses e ON e.TenantId = b.TenantId
                    AND e.Category = b.Category
                    AND e.CreatedAt >= @MonthStart
                  WHERE b.TenantId = @TenantId
                  GROUP BY b.BudgetId, b.DepartmentId, b.Category, b.BudgetAmount, b.AlertThreshold, d.DepartmentName",
                new() { ["TenantId"] = tenantId, ["MonthStart"] = monthStart });

            foreach (System.Data.DataRow row in budgetData.Rows)
            {
                var budgetAmount = (decimal)row["BudgetAmount"];
                var spentAmount = (decimal)row["SpentAmount"];
                var percentUsed = budgetAmount > 0 ? (spentAmount / budgetAmount) * 100 : 0;
                var alertThreshold = (decimal)row["AlertThreshold"];

                if (percentUsed >= alertThreshold)
                {
                    var severity = percentUsed >= 100 ? AnomalySeverity.Critical :
                                  percentUsed >= 95 ? AnomalySeverity.High :
                                  AnomalySeverity.Medium;

                    anomalies.Add(new BudgetAnomaly
                    {
                        BudgetId = (int)row["BudgetId"],
                        DepartmentId = (int?)row["DepartmentId"],
                        DepartmentName = row["DepartmentName"].ToString() ?? "Uncategorized",
                        Category = row["Category"].ToString() ?? "",
                        BudgetAmount = budgetAmount,
                        SpentAmount = spentAmount,
                        PercentUsed = (int)percentUsed,
                        Severity = severity,
                        Description = $"Budget utilization at {percentUsed:F1}% for {row["Category"]}",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            _logger?.LogInformation("Detected {Count} budget anomalies for tenant {TenantId}", anomalies.Count, tenantId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error detecting budget anomalies for tenant {TenantId}", tenantId);
        }

        return anomalies;
    }

    private async Task<List<ExpenseAnomaly>> DetectUnusualUserSpendingAsync(int tenantId, System.Data.DataTable expenseData)
    {
        var anomalies = new List<ExpenseAnomaly>();

        // Group by user and calculate daily averages
        var userGroups = new Dictionary<int, List<decimal>>();

        foreach (System.Data.DataRow row in expenseData.Rows)
        {
            var userId = (int)row["UserId"];
            var amount = (decimal)row["Amount"];

            if (!userGroups.ContainsKey(userId))
                userGroups[userId] = new List<decimal>();

            userGroups[userId].Add(amount);
        }

        // Check each user's spending pattern
        foreach (var userGroup in userGroups.Where(g => g.Value.Count > 3))
        {
            var userAmounts = userGroup.Value;
            var userMean = userAmounts.Average();
            var userStdDev = CalculateStandardDeviation(userAmounts);

            if (userStdDev > 0)
            {
                var threshold = userMean + (2 * userStdDev);

                var highExpenses = expenseData.AsEnumerable()
                    .Where(r => (int)r["UserId"] == userGroup.Key && (decimal)r["Amount"] > threshold)
                    .ToList();

                foreach (var row in highExpenses)
                {
                    anomalies.Add(new ExpenseAnomaly
                    {
                        AnomalyType = AnomalyType.UnusualUserSpending,
                        Severity = AnomalySeverity.Medium,
                        ExpenseId = (int)row["ExpenseId"],
                        UserId = userGroup.Key,
                        UserName = row["UserName"].ToString() ?? "",
                        UserEmail = row["Email"].ToString() ?? "",
                        Category = row["Category"].ToString() ?? "",
                        Amount = (decimal)row["Amount"],
                        Description = $"User's spending pattern differs from their typical behavior",
                        DetectedAt = DateTime.UtcNow,
                        ExpenseDate = (DateTime)row["CreatedAt"]
                    });
                }
            }
        }

        return anomalies;
    }

    private async Task<List<ExpenseAnomaly>> DetectCategoryConcentrationAsync(int tenantId, System.Data.DataTable expenseData)
    {
        var anomalies = new List<ExpenseAnomaly>();

        // Calculate category distribution
        var categoryTotals = new Dictionary<string, decimal>();

        foreach (System.Data.DataRow row in expenseData.Rows)
        {
            var category = row["Category"].ToString() ?? "Other";
            var amount = (decimal)row["Amount"];

            if (!categoryTotals.ContainsKey(category))
                categoryTotals[category] = 0;

            categoryTotals[category] += amount;
        }

        var totalSpent = categoryTotals.Values.Sum();

        if (totalSpent > 0)
        {
            foreach (var category in categoryTotals.Where(c => (c.Value / totalSpent) > 0.5))
            {
                anomalies.Add(new ExpenseAnomaly
                {
                    AnomalyType = AnomalyType.CategoryConcentration,
                    Severity = AnomalySeverity.Low,
                    Category = category.Key,
                    Amount = category.Value,
                    Description = $"Category '{category.Key}' represents {(category.Value / totalSpent * 100):F1}% of recent spending",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        return anomalies;
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private AnomalySeverity CalculateSeverity(decimal amount, decimal threshold)
    {
        var percentAboveThreshold = ((amount - threshold) / threshold) * 100;

        return percentAboveThreshold > 100 ? AnomalySeverity.Critical :
               percentAboveThreshold > 50 ? AnomalySeverity.High :
               AnomalySeverity.Medium;
    }
}

/// <summary>
/// Expense anomaly detection result
/// </summary>
public class ExpenseAnomaly
{
    public int? ExpenseId { get; set; }
    public int? UserId { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public AnomalySeverity Severity { get; set; }
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public DateTime ExpenseDate { get; set; }
}

/// <summary>
/// Budget anomaly detection result
/// </summary>
public class BudgetAnomaly
{
    public int BudgetId { get; set; }
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public int PercentUsed { get; set; }
    public AnomalySeverity Severity { get; set; }
    public string Description { get; set; } = "";
    public DateTime DetectedAt { get; set; }
}

public enum AnomalyType
{
    UnusuallyHighAmount,
    UnusualUserSpending,
    CategoryConcentration,
    FrequentExpenses,
    TimingAnomaly
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}
