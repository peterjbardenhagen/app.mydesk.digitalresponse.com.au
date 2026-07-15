using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Controllers;

[ApiController]
[Route("api/analytics/alerts")]
[Authorize]
public class AnalyticsAlertsController : ControllerBase
{
    private readonly AnomalyDetectionService _anomalyService;
    private readonly AnalyticsNotificationService _notificationService;
    private readonly ILogger<AnalyticsAlertsController> _logger;

    public AnalyticsAlertsController(
        AnomalyDetectionService anomalyService,
        AnalyticsNotificationService notificationService,
        ILogger<AnalyticsAlertsController> logger)
    {
        _anomalyService = anomalyService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get detected expense anomalies for tenant
    /// </summary>
    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenseAnomalies(
        [FromQuery] int tenantId,
        [FromQuery] int daysToAnalyze = 30)
    {
        try
        {
            _logger.LogInformation(
                "Getting expense anomalies for tenant {TenantId} over {Days} days",
                tenantId, daysToAnalyze);

            var anomalies = await _anomalyService.DetectExpenseAnomaliesAsync(tenantId, daysToAnalyze);

            return Ok(new
            {
                anomalies,
                count = anomalies.Count,
                criticalCount = anomalies.FindAll(a => a.Severity == AnomalySeverity.Critical).Count,
                highCount = anomalies.FindAll(a => a.Severity == AnomalySeverity.High).Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense anomalies for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detected budget anomalies for tenant
    /// </summary>
    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudgetAnomalies([FromQuery] int tenantId)
    {
        try
        {
            _logger.LogInformation("Getting budget anomalies for tenant {TenantId}", tenantId);

            var anomalies = await _anomalyService.DetectBudgetAnomaliesAsync(tenantId);

            return Ok(new
            {
                anomalies,
                count = anomalies.Count,
                criticalCount = anomalies.FindAll(a => a.Severity == AnomalySeverity.Critical).Count,
                overBudgetCount = anomalies.FindAll(a => a.PercentUsed >= 100).Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting budget anomalies for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger anomaly detection and notification check for tenant
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> TriggerAnomalyCheck([FromQuery] int tenantId)
    {
        try
        {
            _logger.LogInformation("Triggering anomaly check for tenant {TenantId}", tenantId);

            await _notificationService.CheckAndNotifyAsync(tenantId);

            return Ok(new { message = "Anomaly check completed and notifications sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during anomaly check for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get anomaly summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetAnomalySummary([FromQuery] int tenantId)
    {
        try
        {
            _logger.LogInformation("Getting anomaly summary for tenant {TenantId}", tenantId);

            var expenseAnomalies = await _anomalyService.DetectExpenseAnomaliesAsync(tenantId, 7);
            var budgetAnomalies = await _anomalyService.DetectBudgetAnomaliesAsync(tenantId);

            var summary = new
            {
                totalAlerts = expenseAnomalies.Count + budgetAnomalies.Count,
                expenseAnomalies = new
                {
                    total = expenseAnomalies.Count,
                    critical = expenseAnomalies.FindAll(a => a.Severity == AnomalySeverity.Critical).Count,
                    high = expenseAnomalies.FindAll(a => a.Severity == AnomalySeverity.High).Count,
                    medium = expenseAnomalies.FindAll(a => a.Severity == AnomalySeverity.Medium).Count
                },
                budgetAnomalies = new
                {
                    total = budgetAnomalies.Count,
                    critical = budgetAnomalies.FindAll(a => a.Severity == AnomalySeverity.Critical).Count,
                    high = budgetAnomalies.FindAll(a => a.Severity == AnomalySeverity.High).Count,
                    overBudget = budgetAnomalies.FindAll(a => a.PercentUsed >= 100).Count
                },
                recentAnomalies = expenseAnomalies.Count > 0 ? expenseAnomalies.GetRange(0, Math.Min(5, expenseAnomalies.Count)) : new List<ExpenseAnomaly>(),
                lastChecked = DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting anomaly summary for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
