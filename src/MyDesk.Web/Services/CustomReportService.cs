using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for managing custom dashboard report definitions.
/// Allows users to configure which metrics, charts, and data to include in their reports.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class CustomReportService
{
    private readonly DatabaseService _db;
    private readonly ILogger<CustomReportService>? _logger;

    public CustomReportService(
        DatabaseService db,
        ILogger<CustomReportService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Create a custom report template
    /// </summary>
    public async Task<CustomReportTemplate> CreateTemplateAsync(
        int tenantId,
        int userId,
        string name,
        string dashboardType,
        CustomReportSettings settings)
    {
        _logger?.LogInformation(
            "Creating custom report template: Name={Name}, Type={Type}, TenantId={TenantId}, UserId={UserId}",
            name, dashboardType, tenantId, userId);

        var templateId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO CustomReportTemplates (TenantId, UserId, Name, DashboardType, IncludeSummary, IncludeCharts, IncludeDetailed, IncludeAnalysis, IsDefault, CreatedAt)
              VALUES (@TenantId, @UserId, @Name, @DashboardType, @IncludeSummary, @IncludeCharts, @IncludeDetailed, @IncludeAnalysis, @IsDefault, GETUTCDATE())
              SELECT @@IDENTITY",
            new()
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["Name"] = name,
                ["DashboardType"] = dashboardType,
                ["IncludeSummary"] = settings.IncludeSummary,
                ["IncludeCharts"] = settings.IncludeCharts,
                ["IncludeDetailed"] = settings.IncludeDetailed,
                ["IncludeAnalysis"] = settings.IncludeAnalysis,
                ["IsDefault"] = false
            });

        _logger?.LogInformation("Created custom report template: TemplateId={TemplateId}", templateId);

        return new CustomReportTemplate
        {
            TemplateId = templateId,
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            DashboardType = dashboardType,
            Settings = settings,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get custom report template by ID
    /// </summary>
    public async Task<CustomReportTemplate?> GetTemplateAsync(int tenantId, int templateId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT TemplateId, TenantId, UserId, Name, DashboardType, IncludeSummary, IncludeCharts, IncludeDetailed, IncludeAnalysis, IsDefault, CreatedAt
              FROM CustomReportTemplates
              WHERE TenantId = @TenantId AND TemplateId = @TemplateId",
            new() { ["TenantId"] = tenantId, ["TemplateId"] = templateId });

        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return MapToTemplate(row);
    }

    /// <summary>
    /// Get all custom report templates for a user
    /// </summary>
    public async Task<List<CustomReportTemplate>> GetUserTemplatesAsync(int tenantId, int userId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT TemplateId, TenantId, UserId, Name, DashboardType, IncludeSummary, IncludeCharts, IncludeDetailed, IncludeAnalysis, IsDefault, CreatedAt
              FROM CustomReportTemplates
              WHERE TenantId = @TenantId AND UserId = @UserId
              ORDER BY IsDefault DESC, CreatedAt DESC",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        var templates = new List<CustomReportTemplate>();
        foreach (System.Data.DataRow row in dt.Rows)
        {
            templates.Add(MapToTemplate(row));
        }
        return templates;
    }

    /// <summary>
    /// Update custom report template
    /// </summary>
    public async Task<bool> UpdateTemplateAsync(
        int tenantId,
        int templateId,
        string name,
        CustomReportSettings settings)
    {
        _logger?.LogInformation(
            "Updating custom report template: TemplateId={TemplateId}, Name={Name}",
            templateId, name);

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"UPDATE CustomReportTemplates
              SET Name = @Name, IncludeSummary = @IncludeSummary, IncludeCharts = @IncludeCharts,
                  IncludeDetailed = @IncludeDetailed, IncludeAnalysis = @IncludeAnalysis
              WHERE TenantId = @TenantId AND TemplateId = @TemplateId",
            new()
            {
                ["TenantId"] = tenantId,
                ["TemplateId"] = templateId,
                ["Name"] = name,
                ["IncludeSummary"] = settings.IncludeSummary,
                ["IncludeCharts"] = settings.IncludeCharts,
                ["IncludeDetailed"] = settings.IncludeDetailed,
                ["IncludeAnalysis"] = settings.IncludeAnalysis
            });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Set as default template for user
    /// </summary>
    public async Task<bool> SetDefaultTemplateAsync(int tenantId, int userId, int templateId)
    {
        _logger?.LogInformation(
            "Setting default template: UserId={UserId}, TemplateId={TemplateId}",
            userId, templateId);

        await _db.ExecuteNonQueryAsync(
            @"UPDATE CustomReportTemplates
              SET IsDefault = 0
              WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"UPDATE CustomReportTemplates
              SET IsDefault = 1
              WHERE TenantId = @TenantId AND TemplateId = @TemplateId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["TemplateId"] = templateId, ["UserId"] = userId });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete custom report template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int tenantId, int templateId)
    {
        _logger?.LogInformation(
            "Deleting custom report template: TemplateId={TemplateId}",
            templateId);

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"DELETE FROM CustomReportTemplates
              WHERE TenantId = @TenantId AND TemplateId = @TemplateId",
            new() { ["TenantId"] = tenantId, ["TemplateId"] = templateId });

        return rowsAffected > 0;
    }

    private CustomReportTemplate MapToTemplate(System.Data.DataRow row)
    {
        return new CustomReportTemplate
        {
            TemplateId = (int)row["TemplateId"],
            TenantId = (int)row["TenantId"],
            UserId = (int)row["UserId"],
            Name = row["Name"].ToString() ?? "",
            DashboardType = row["DashboardType"].ToString() ?? "executive",
            Settings = new CustomReportSettings
            {
                IncludeSummary = (bool)row["IncludeSummary"],
                IncludeCharts = (bool)row["IncludeCharts"],
                IncludeDetailed = (bool)row["IncludeDetailed"],
                IncludeAnalysis = (bool)row["IncludeAnalysis"]
            },
            IsDefault = (bool)row["IsDefault"],
            CreatedAt = (DateTime)row["CreatedAt"]
        };
    }
}

/// <summary>
/// Custom report template definition
/// </summary>
public class CustomReportTemplate
{
    public int TemplateId { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string DashboardType { get; set; } = "executive";
    public CustomReportSettings Settings { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Settings for custom report generation
/// </summary>
public class CustomReportSettings
{
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetailed { get; set; } = true;
    public bool IncludeAnalysis { get; set; } = false;
}

/// <summary>
/// Request to create custom report template
/// </summary>
public class CreateCustomReportRequest
{
    public string Name { get; set; } = "";
    public string DashboardType { get; set; } = "executive";
    public CustomReportSettings Settings { get; set; } = new();
}
