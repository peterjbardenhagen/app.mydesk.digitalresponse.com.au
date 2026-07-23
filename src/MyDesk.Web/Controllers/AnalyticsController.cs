using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDesk.Web.Services;

namespace MyDesk.Web.Controllers;

/// <summary>
/// API endpoints for dashboard and analytics data.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get executive dashboard metrics (CFO view)
    /// </summary>
    [HttpGet("executive-dashboard")]
    public async Task<IActionResult> GetExecutiveDashboard([FromQuery] int tenantId)
    {
        // Validate user has CFO/Admin role for this tenant
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!userRoles.Contains("CFO") && !userRoles.Contains("Admin"))
        {
            return Forbid();
        }
        var dashboard = await _analyticsService.GetExecutiveDashboardAsync(tenantId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get manager dashboard metrics (Team view)
    /// </summary>
    [HttpGet("manager-dashboard")]
    public async Task<IActionResult> GetManagerDashboard([FromQuery] int tenantId, [FromQuery] int managerId)
    {
        // Validate user is this manager or has admin role
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!userRoles.Contains("Admin") && !userId.Equals(managerId.ToString()))
        {
            return Forbid();
        }
        var dashboard = await _analyticsService.GetManagerDashboardAsync(tenantId, managerId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get employee dashboard metrics (Personal view)
    /// </summary>
    [HttpGet("employee-dashboard")]
    public async Task<IActionResult> GetEmployeeDashboard([FromQuery] int tenantId, [FromQuery] int userId)
    {
        // Validate user is this employee or has admin role
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!userRoles.Contains("Admin") && !userId.Equals(userIdClaim))
        {
            return Forbid();
        }
        var dashboard = await _analyticsService.GetEmployeeDashboardAsync(tenantId, userId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Export dashboard data as CSV
    /// </summary>
    [HttpGet("export-csv")]
    public IActionResult ExportCsv([FromQuery] int tenantId, [FromQuery] string dashboardType = "executive")
    {
        // TODO: Implement CSV export
        return BadRequest("Export functionality not yet implemented");
    }

    /// <summary>
    /// Export dashboard data as PDF
    /// </summary>
    [HttpGet("export-pdf")]
    public IActionResult ExportPdf([FromQuery] int tenantId, [FromQuery] string dashboardType = "executive")
    {
        // TODO: Implement PDF export using QuestPDF
        return BadRequest("Export functionality not yet implemented");
    }
}
