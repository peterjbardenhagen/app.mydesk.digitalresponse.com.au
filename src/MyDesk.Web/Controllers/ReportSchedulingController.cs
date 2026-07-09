using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportSchedulingController : ControllerBase
{
    private readonly DashboardReportScheduleService _scheduleService;
    private readonly ILogger<ReportSchedulingController> _logger;

    public ReportSchedulingController(
        DashboardReportScheduleService scheduleService,
        ILogger<ReportSchedulingController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a recurring dashboard report
    /// </summary>
    /// <remarks>
    /// Cron expression examples:
    /// - "0 8 * * MON" = Every Monday at 8:00 AM
    /// - "0 9 * * *" = Every day at 9:00 AM
    /// - "0 0 1 * *" = First day of month at midnight
    /// - "0 8 1-7 * MON" = First Monday of month at 8:00 AM
    /// </remarks>
    [HttpPost("schedule")]
    public IActionResult ScheduleReport(
        [FromQuery] int tenantId,
        [FromBody] ScheduleDashboardReportRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Scheduling {DashboardType} report for tenant {TenantId} to {Email} with cron {Cron}",
                request.DashboardType, tenantId, request.Email, request.CronExpression);

            var jobId = request.DashboardType.ToLower() switch
            {
                "executive" => _scheduleService.ScheduleExecutiveDashboardReport(
                    tenantId, request.Email, request.CronExpression,
                    request.Format, request.IncludeSummary, request.IncludeDetailed),

                "manager" => _scheduleService.ScheduleManagerDashboardReport(
                    tenantId, request.ManagerId ?? 0, request.Email, request.CronExpression,
                    request.Format, request.IncludeSummary, request.IncludeDetailed),

                "employee" => _scheduleService.ScheduleEmployeeDashboardReport(
                    tenantId, request.UserId ?? 0, request.Email, request.CronExpression,
                    request.Format, request.IncludeSummary, request.IncludeDetailed),

                _ => throw new ArgumentException($"Unsupported dashboard type: {request.DashboardType}")
            };

            return Ok(new
            {
                jobId,
                message = $"Report scheduled successfully",
                dashboardType = request.DashboardType,
                email = request.Email,
                cronExpression = request.CronExpression,
                format = request.Format
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling report for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a scheduled report
    /// </summary>
    [HttpDelete("schedule/{jobId}")]
    public IActionResult UnscheduleReport(string jobId)
    {
        try
        {
            _logger.LogInformation("Removing scheduled report: {JobId}", jobId);
            _scheduleService.UnscheduleReport(jobId);
            return Ok(new { message = "Report unscheduled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing scheduled report: {JobId}", jobId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
