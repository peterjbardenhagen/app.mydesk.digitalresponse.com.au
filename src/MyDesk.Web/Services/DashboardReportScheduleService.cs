using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for scheduling recurring dashboard reports via Hangfire.
/// Supports email delivery of dashboards on customizable schedules.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class DashboardReportScheduleService
{
    private readonly DashboardExportService _exportService;
    private readonly EmailService _emailService;
    private readonly ILogger<DashboardReportScheduleService>? _logger;

    public DashboardReportScheduleService(
        DashboardExportService exportService,
        EmailService emailService,
        ILogger<DashboardReportScheduleService>? logger = null)
    {
        _exportService = exportService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Schedule recurring executive dashboard report via email
    /// </summary>
    public string ScheduleExecutiveDashboardReport(
        int tenantId,
        string email,
        string cronExpression,
        string format = "pdf",
        bool includeSummary = true,
        bool includeDetailed = true)
    {
        _logger?.LogInformation(
            "Scheduling executive dashboard report for tenant {TenantId} to {Email} with cron {Cron}",
            tenantId, email, cronExpression);

        var jobId = RecurringJob.AddOrUpdate(
            $"executive-dashboard-{tenantId}-{email}",
            () => SendExecutiveDashboardReportAsync(tenantId, email, format, includeSummary, includeDetailed),
            cronExpression,
            TimeZoneInfo.Utc);

        _logger?.LogInformation("Scheduled job: {JobId}", jobId);
        return jobId;
    }

    /// <summary>
    /// Schedule recurring manager dashboard report via email
    /// </summary>
    public string ScheduleManagerDashboardReport(
        int tenantId,
        int managerId,
        string email,
        string cronExpression,
        string format = "pdf",
        bool includeSummary = true,
        bool includeDetailed = true)
    {
        _logger?.LogInformation(
            "Scheduling manager dashboard report for manager {ManagerId} in tenant {TenantId} to {Email} with cron {Cron}",
            managerId, tenantId, email, cronExpression);

        var jobId = RecurringJob.AddOrUpdate(
            $"manager-dashboard-{tenantId}-{managerId}-{email}",
            () => SendManagerDashboardReportAsync(tenantId, managerId, email, format, includeSummary, includeDetailed),
            cronExpression,
            TimeZoneInfo.Utc);

        _logger?.LogInformation("Scheduled job: {JobId}", jobId);
        return jobId;
    }

    /// <summary>
    /// Schedule recurring employee dashboard report via email
    /// </summary>
    public string ScheduleEmployeeDashboardReport(
        int tenantId,
        int userId,
        string email,
        string cronExpression,
        string format = "pdf",
        bool includeSummary = true,
        bool includeDetailed = true)
    {
        _logger?.LogInformation(
            "Scheduling employee dashboard report for user {UserId} in tenant {TenantId} to {Email} with cron {Cron}",
            userId, tenantId, email, cronExpression);

        var jobId = RecurringJob.AddOrUpdate(
            $"employee-dashboard-{tenantId}-{userId}-{email}",
            () => SendEmployeeDashboardReportAsync(tenantId, userId, email, format, includeSummary, includeDetailed),
            cronExpression,
            TimeZoneInfo.Utc);

        _logger?.LogInformation("Scheduled job: {JobId}", jobId);
        return jobId;
    }

    /// <summary>
    /// Remove scheduled report
    /// </summary>
    public void UnscheduleReport(string jobId)
    {
        _logger?.LogInformation("Removing scheduled job: {JobId}", jobId);
        RecurringJob.RemoveIfExists(jobId);
    }

    /// <summary>
    /// Send executive dashboard report (called by Hangfire job)
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendExecutiveDashboardReportAsync(
        int tenantId,
        string email,
        string format,
        bool includeSummary,
        bool includeDetailed)
    {
        try
        {
            _logger?.LogInformation(
                "Sending executive dashboard report for tenant {TenantId} to {Email} in format {Format}",
                tenantId, email, format);

            byte[] reportData = format.ToLower() switch
            {
                "csv" => await _exportService.ExportExecutiveDashboardAsCSVAsync(
                    tenantId, true, includeDetailed, includeSummary),
                "json" => await _exportService.ExportExecutiveDashboardAsJsonAsync(
                    tenantId, true, includeDetailed, includeSummary),
                "pdf" => await _exportService.ExportExecutiveDashboardAsPdfAsync(
                    tenantId, true, includeDetailed, includeSummary),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            var fileName = $"executive-dashboard-{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";
            var subject = $"Executive Dashboard Report - {DateTime.UtcNow:MMMM d, yyyy}";
            var body = $"Please find attached the executive dashboard report for {DateTime.UtcNow:MMMM d, yyyy}.";

            await _emailService.SendAsync(email, subject, body);

            _logger?.LogInformation("Successfully sent executive dashboard report to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send executive dashboard report for tenant {TenantId} to {Email}",
                tenantId, email);
            throw;
        }
    }

    /// <summary>
    /// Send manager dashboard report (called by Hangfire job)
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendManagerDashboardReportAsync(
        int tenantId,
        int managerId,
        string email,
        string format,
        bool includeSummary,
        bool includeDetailed)
    {
        try
        {
            _logger?.LogInformation(
                "Sending manager dashboard report for manager {ManagerId} in tenant {TenantId} to {Email} in format {Format}",
                managerId, tenantId, email, format);

            byte[] reportData = format.ToLower() switch
            {
                "csv" => await _exportService.ExportManagerDashboardAsCSVAsync(
                    tenantId, managerId, true, includeDetailed, includeSummary),
                "json" => await _exportService.ExportManagerDashboardAsJsonAsync(
                    tenantId, managerId, true, includeDetailed, includeSummary),
                "pdf" => await _exportService.ExportManagerDashboardAsPdfAsync(
                    tenantId, managerId, true, includeDetailed, includeSummary),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            var fileName = $"manager-dashboard-{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";
            var subject = $"Manager Dashboard Report - {DateTime.UtcNow:MMMM d, yyyy}";
            var body = $"Please find attached the manager dashboard report for {DateTime.UtcNow:MMMM d, yyyy}.";

            await _emailService.SendAsync(email, subject, body);

            _logger?.LogInformation(
                "Successfully sent manager dashboard report to {Email} for manager {ManagerId}",
                email, managerId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to send manager dashboard report for manager {ManagerId} in tenant {TenantId} to {Email}",
                managerId, tenantId, email);
            throw;
        }
    }

    /// <summary>
    /// Send employee dashboard report (called by Hangfire job)
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SendEmployeeDashboardReportAsync(
        int tenantId,
        int userId,
        string email,
        string format,
        bool includeSummary,
        bool includeDetailed)
    {
        try
        {
            _logger?.LogInformation(
                "Sending employee dashboard report for user {UserId} in tenant {TenantId} to {Email} in format {Format}",
                userId, tenantId, email, format);

            byte[] reportData = format.ToLower() switch
            {
                "csv" => await _exportService.ExportEmployeeDashboardAsCSVAsync(
                    tenantId, userId, true, includeDetailed, includeSummary),
                "json" => await _exportService.ExportEmployeeDashboardAsJsonAsync(
                    tenantId, userId, true, includeDetailed, includeSummary),
                "pdf" => await _exportService.ExportEmployeeDashboardAsPdfAsync(
                    tenantId, userId, true, includeDetailed, includeSummary),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            var fileName = $"my-dashboard-{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";
            var subject = $"My Dashboard Report - {DateTime.UtcNow:MMMM d, yyyy}";
            var body = $"Please find attached your personal dashboard report for {DateTime.UtcNow:MMMM d, yyyy}.";

            await _emailService.SendAsync(email, subject, body);

            _logger?.LogInformation("Successfully sent employee dashboard report to {Email} for user {UserId}",
                email, userId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to send employee dashboard report for user {UserId} in tenant {TenantId} to {Email}",
                userId, tenantId, email);
            throw;
        }
    }
}

/// <summary>
/// DTO for creating scheduled dashboard report
/// </summary>
public class ScheduleDashboardReportRequest
{
    public string DashboardType { get; set; } // executive, manager, employee
    public string Email { get; set; }
    public string CronExpression { get; set; } // e.g., "0 8 * * MON" for weekly Monday 8am
    public string Format { get; set; } = "pdf"; // pdf, csv, json
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeDetailed { get; set; } = true;

    // For manager dashboard
    public int? ManagerId { get; set; }

    // For employee dashboard
    public int? UserId { get; set; }
}
