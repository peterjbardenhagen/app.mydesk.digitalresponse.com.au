using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
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

    [HttpPost("executive/export")]
    public async Task<IActionResult> ExportExecutiveDashboardAsync(
        [FromQuery] int tenantId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting executive dashboard for tenant {TenantId} in format {Format}",
                tenantId, request.Format);

            byte[] fileContent = request.Format.ToLower() switch
            {
                "csv" => await _exportService.ExportExecutiveDashboardAsCSVAsync(
                    tenantId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "json" => await _exportService.ExportExecutiveDashboardAsJsonAsync(
                    tenantId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "pdf" => await _exportService.ExportExecutiveDashboardAsPdfAsync(
                    tenantId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };

            var contentType = request.Format.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var fileName = $"executive-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{request.Format.ToLower()}";
            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting executive dashboard for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("manager/export")]
    public async Task<IActionResult> ExportManagerDashboardAsync(
        [FromQuery] int tenantId,
        [FromQuery] int managerId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting manager dashboard for manager {ManagerId} in tenant {TenantId} in format {Format}",
                managerId, tenantId, request.Format);

            byte[] fileContent = request.Format.ToLower() switch
            {
                "csv" => await _exportService.ExportManagerDashboardAsCSVAsync(
                    tenantId, managerId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "json" => await _exportService.ExportManagerDashboardAsJsonAsync(
                    tenantId, managerId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "pdf" => await _exportService.ExportManagerDashboardAsPdfAsync(
                    tenantId, managerId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };

            var contentType = request.Format.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var fileName = $"manager-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{request.Format.ToLower()}";
            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting manager dashboard for manager {ManagerId} in tenant {TenantId}",
                managerId, tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("employee/export")]
    public async Task<IActionResult> ExportEmployeeDashboardAsync(
        [FromQuery] int tenantId,
        [FromQuery] int userId,
        [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting employee dashboard for user {UserId} in tenant {TenantId} in format {Format}",
                userId, tenantId, request.Format);

            byte[] fileContent = request.Format.ToLower() switch
            {
                "csv" => await _exportService.ExportEmployeeDashboardAsCSVAsync(
                    tenantId, userId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "json" => await _exportService.ExportEmployeeDashboardAsJsonAsync(
                    tenantId, userId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                "pdf" => await _exportService.ExportEmployeeDashboardAsPdfAsync(
                    tenantId, userId, request.IncludeCharts, request.IncludeDetailed, request.IncludeSummary),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };

            var contentType = request.Format.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var fileName = $"my-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{request.Format.ToLower()}";
            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting employee dashboard for user {UserId} in tenant {TenantId}",
                userId, tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class ExportRequest
{
    public string Format { get; set; } = "csv"; // csv, json, pdf
    public string FileName { get; set; }
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetailed { get; set; } = true;
    public bool IncludeSummary { get; set; } = true;
}
