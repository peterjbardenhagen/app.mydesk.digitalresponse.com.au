using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Controllers;

[ApiController]
[Route("api/custom-reports")]
[Authorize]
public class CustomReportController : ControllerBase
{
    private readonly CustomReportService _customReportService;
    private readonly ILogger<CustomReportController> _logger;

    public CustomReportController(
        CustomReportService customReportService,
        ILogger<CustomReportController> logger)
    {
        _customReportService = customReportService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's custom report templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetUserTemplates([FromQuery] int tenantId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation(
                "Getting custom report templates for user {UserId} in tenant {TenantId}",
                userId, tenantId);

            var templates = await _customReportService.GetUserTemplatesAsync(tenantId, userId);
            return Ok(new { templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom report templates for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get specific custom report template
    /// </summary>
    [HttpGet("templates/{templateId}")]
    public async Task<IActionResult> GetTemplate(
        [FromQuery] int tenantId,
        int templateId)
    {
        try
        {
            _logger.LogInformation(
                "Getting custom report template: TemplateId={TemplateId}, TenantId={TenantId}",
                templateId, tenantId);

            var template = await _customReportService.GetTemplateAsync(tenantId, templateId);
            if (template == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(new { template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom report template {TemplateId}", templateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create new custom report template
    /// </summary>
    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate(
        [FromQuery] int tenantId,
        [FromBody] CreateCustomReportRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation(
                "Creating custom report template: Name={Name}, Type={Type}, TenantId={TenantId}, UserId={UserId}",
                request.Name, request.DashboardType, tenantId, userId);

            var template = await _customReportService.CreateTemplateAsync(
                tenantId, userId, request.Name, request.DashboardType, request.Settings);

            return Created(
                $"/api/custom-reports/templates/{template.TemplateId}",
                new { template, message = "Template created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom report template for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update custom report template
    /// </summary>
    [HttpPut("templates/{templateId}")]
    public async Task<IActionResult> UpdateTemplate(
        [FromQuery] int tenantId,
        int templateId,
        [FromBody] CreateCustomReportRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Updating custom report template: TemplateId={TemplateId}, Name={Name}",
                templateId, request.Name);

            var success = await _customReportService.UpdateTemplateAsync(
                tenantId, templateId, request.Name, request.Settings);

            if (!success)
            {
                return NotFound(new { error = "Template not found" });
            }

            var template = await _customReportService.GetTemplateAsync(tenantId, templateId);
            return Ok(new { template, message = "Template updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating custom report template {TemplateId}", templateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set template as default for current user
    /// </summary>
    [HttpPut("templates/{templateId}/set-default")]
    public async Task<IActionResult> SetDefaultTemplate(
        [FromQuery] int tenantId,
        int templateId)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation(
                "Setting default template: UserId={UserId}, TemplateId={TemplateId}",
                userId, templateId);

            var success = await _customReportService.SetDefaultTemplateAsync(tenantId, userId, templateId);
            if (!success)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(new { message = "Template set as default" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default template {TemplateId}", templateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete custom report template
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(
        [FromQuery] int tenantId,
        int templateId)
    {
        try
        {
            _logger.LogInformation(
                "Deleting custom report template: TemplateId={TemplateId}, TenantId={TenantId}",
                templateId, tenantId);

            var success = await _customReportService.DeleteTemplateAsync(tenantId, templateId);
            if (!success)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(new { message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting custom report template {TemplateId}", templateId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
