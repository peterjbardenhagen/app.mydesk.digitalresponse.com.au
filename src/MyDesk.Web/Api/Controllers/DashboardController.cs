using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Dashboard Controller: Export and analytics endpoints
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardExportService _exportService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DashboardExportService exportService,
        ILogger<DashboardController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Export executive dashboard
    /// </summary>
    [HttpPost("executive/export")]
    public async Task<IActionResult> ExportExecutiveDashboard(
        int tenantId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Exporting executive dashboard as {Format} for tenant {TenantId}",
                request.Format, tenantId);

            byte[] fileContent;
            string contentType;
            string fileName;

            switch (request.Format?.ToLower())
            {
                case "json":
                    fileContent = await _exportService.ExportExecutiveDashboardAsJsonAsync(
                        tenantId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "application/json";
                    fileName = $"{request.FileName ?? "executive-dashboard"}.json";
                    break;

                case "csv":
                default:
                    fileContent = await _exportService.ExportExecutiveDashboardAsCSVAsync(
                        tenantId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "text/csv";
                    fileName = $"{request.FileName ?? "executive-dashboard"}.csv";
                    break;
            }

            return File(
                fileContent,
                contentType,
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting executive dashboard");
            return BadRequest(new { error = "Failed to export dashboard", detail = ex.Message });
        }
    }

    /// <summary>
    /// Export manager dashboard
    /// </summary>
    [HttpPost("manager/export")]
    public async Task<IActionResult> ExportManagerDashboard(
        int tenantId,
        int managerId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Exporting manager dashboard as {Format} for tenant {TenantId}, manager {ManagerId}",
                request.Format, tenantId, managerId);

            byte[] fileContent;
            string contentType;
            string fileName;

            switch (request.Format?.ToLower())
            {
                case "json":
                    fileContent = await _exportService.ExportManagerDashboardAsJsonAsync(
                        tenantId,
                        managerId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "application/json";
                    fileName = $"{request.FileName ?? "manager-dashboard"}.json";
                    break;

                case "csv":
                default:
                    fileContent = await _exportService.ExportManagerDashboardAsCSVAsync(
                        tenantId,
                        managerId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "text/csv";
                    fileName = $"{request.FileName ?? "manager-dashboard"}.csv";
                    break;
            }

            return File(
                fileContent,
                contentType,
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting manager dashboard");
            return BadRequest(new { error = "Failed to export dashboard", detail = ex.Message });
        }
    }

    /// <summary>
    /// Export employee dashboard
    /// </summary>
    [HttpPost("employee/export")]
    public async Task<IActionResult> ExportEmployeeDashboard(
        int tenantId,
        int userId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Exporting employee dashboard as {Format} for tenant {TenantId}, user {UserId}",
                request.Format, tenantId, userId);

            byte[] fileContent;
            string contentType;
            string fileName;

            switch (request.Format?.ToLower())
            {
                case "json":
                    fileContent = await _exportService.ExportEmployeeDashboardAsJsonAsync(
                        tenantId,
                        userId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "application/json";
                    fileName = $"{request.FileName ?? "employee-dashboard"}.json";
                    break;

                case "csv":
                default:
                    fileContent = await _exportService.ExportEmployeeDashboardAsCSVAsync(
                        tenantId,
                        userId,
                        request.IncludeCharts,
                        request.IncludeDetailed,
                        request.IncludeSummary);
                    contentType = "text/csv";
                    fileName = $"{request.FileName ?? "employee-dashboard"}.csv";
                    break;
            }

            return File(
                fileContent,
                contentType,
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting employee dashboard");
            return BadRequest(new { error = "Failed to export dashboard", detail = ex.Message });
        }
    }
}

/// <summary>
/// Request model for dashboard export
/// </summary>
public class ExportRequest
{
    public string Format { get; set; } = "csv";  // csv, json, pdf
    public string? FileName { get; set; }
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetailed { get; set; } = true;
    public bool IncludeSummary { get; set; } = true;
}
