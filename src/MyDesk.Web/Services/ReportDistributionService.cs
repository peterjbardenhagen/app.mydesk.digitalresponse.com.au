using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for managing report distribution lists.
/// Allows users to specify recipients and schedules for automated report delivery.
/// Part of Phase 6: Dashboard & Analytics
/// </summary>
public class ReportDistributionService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ReportDistributionService>? _logger;

    public ReportDistributionService(
        DatabaseService db,
        ILogger<ReportDistributionService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Create a report distribution list
    /// </summary>
    public async Task<ReportDistributionList> CreateDistributionListAsync(
        int tenantId,
        int userId,
        string name,
        string description = "")
    {
        _logger?.LogInformation(
            "Creating report distribution list: Name={Name}, TenantId={TenantId}, UserId={UserId}",
            name, tenantId, userId);

        var listId = await _db.ExecuteScalarAsync(
            @"INSERT INTO ReportDistributionLists (TenantId, CreatedBy, Name, Description, IsActive, CreatedAt)
              VALUES (@TenantId, @CreatedBy, @Name, @Description, 1, GETUTCDATE())
              SELECT @@IDENTITY",
            new()
            {
                ["TenantId"] = tenantId,
                ["CreatedBy"] = userId,
                ["Name"] = name,
                ["Description"] = description
            });

        _logger?.LogInformation("Created report distribution list: ListId={ListId}", listId);

        return new ReportDistributionList
        {
            ListId = (int)(decimal)listId,
            TenantId = tenantId,
            CreatedBy = userId,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Recipients = new List<ReportRecipient>()
        };
    }

    /// <summary>
    /// Get distribution list with recipients
    /// </summary>
    public async Task<ReportDistributionList?> GetDistributionListAsync(int tenantId, int listId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT ListId, TenantId, CreatedBy, Name, Description, IsActive, CreatedAt
              FROM ReportDistributionLists
              WHERE TenantId = @TenantId AND ListId = @ListId",
            new() { ["TenantId"] = tenantId, ["ListId"] = listId });

        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        var list = new ReportDistributionList
        {
            ListId = (int)row["ListId"],
            TenantId = (int)row["TenantId"],
            CreatedBy = (int)row["CreatedBy"],
            Name = row["Name"].ToString() ?? "",
            Description = row["Description"].ToString() ?? "",
            IsActive = (bool)row["IsActive"],
            CreatedAt = (DateTime)row["CreatedAt"],
            Recipients = new List<ReportRecipient>()
        };

        // Get recipients
        var recipientDt = await _db.QueryAsync(
            @"SELECT RecipientId, ListId, Email, Name, IsActive
              FROM ReportDistributionRecipients
              WHERE ListId = @ListId AND IsActive = 1",
            new() { ["ListId"] = listId });

        foreach (System.Data.DataRow recipientRow in recipientDt.Rows)
        {
            list.Recipients.Add(new ReportRecipient
            {
                RecipientId = (int)recipientRow["RecipientId"],
                ListId = (int)recipientRow["ListId"],
                Email = recipientRow["Email"].ToString() ?? "",
                Name = recipientRow["Name"].ToString() ?? "",
                IsActive = (bool)recipientRow["IsActive"]
            });
        }

        return list;
    }

    /// <summary>
    /// Get all distribution lists for tenant
    /// </summary>
    public async Task<List<ReportDistributionList>> GetTenantDistributionListsAsync(int tenantId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT ListId, TenantId, CreatedBy, Name, Description, IsActive, CreatedAt
              FROM ReportDistributionLists
              WHERE TenantId = @TenantId AND IsActive = 1
              ORDER BY CreatedAt DESC",
            new() { ["TenantId"] = tenantId });

        var lists = new List<ReportDistributionList>();
        foreach (System.Data.DataRow row in dt.Rows)
        {
            lists.Add(new ReportDistributionList
            {
                ListId = (int)row["ListId"],
                TenantId = (int)row["TenantId"],
                CreatedBy = (int)row["CreatedBy"],
                Name = row["Name"].ToString() ?? "",
                Description = row["Description"].ToString() ?? "",
                IsActive = (bool)row["IsActive"],
                CreatedAt = (DateTime)row["CreatedAt"],
                Recipients = new List<ReportRecipient>()
            });
        }

        return lists;
    }

    /// <summary>
    /// Add recipient to distribution list
    /// </summary>
    public async Task<ReportRecipient> AddRecipientAsync(
        int listId,
        string email,
        string name = "")
    {
        _logger?.LogInformation(
            "Adding recipient to distribution list: ListId={ListId}, Email={Email}",
            listId, email);

        var recipientId = await _db.ExecuteScalarAsync(
            @"INSERT INTO ReportDistributionRecipients (ListId, Email, Name, IsActive)
              VALUES (@ListId, @Email, @Name, 1)
              SELECT @@IDENTITY",
            new()
            {
                ["ListId"] = listId,
                ["Email"] = email,
                ["Name"] = string.IsNullOrEmpty(name) ? email : name
            });

        return new ReportRecipient
        {
            RecipientId = (int)(decimal)recipientId,
            ListId = listId,
            Email = email,
            Name = string.IsNullOrEmpty(name) ? email : name,
            IsActive = true
        };
    }

    /// <summary>
    /// Remove recipient from distribution list
    /// </summary>
    public async Task<bool> RemoveRecipientAsync(int listId, int recipientId)
    {
        _logger?.LogInformation(
            "Removing recipient from distribution list: ListId={ListId}, RecipientId={RecipientId}",
            listId, recipientId);

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"UPDATE ReportDistributionRecipients
              SET IsActive = 0
              WHERE ListId = @ListId AND RecipientId = @RecipientId",
            new() { ["ListId"] = listId, ["RecipientId"] = recipientId });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Update distribution list
    /// </summary>
    public async Task<bool> UpdateDistributionListAsync(
        int tenantId,
        int listId,
        string name,
        string description = "")
    {
        _logger?.LogInformation(
            "Updating distribution list: ListId={ListId}, Name={Name}",
            listId, name);

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"UPDATE ReportDistributionLists
              SET Name = @Name, Description = @Description
              WHERE TenantId = @TenantId AND ListId = @ListId",
            new()
            {
                ["TenantId"] = tenantId,
                ["ListId"] = listId,
                ["Name"] = name,
                ["Description"] = description
            });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete distribution list
    /// </summary>
    public async Task<bool> DeleteDistributionListAsync(int tenantId, int listId)
    {
        _logger?.LogInformation(
            "Deleting distribution list: ListId={ListId}",
            listId);

        var rowsAffected = await _db.ExecuteNonQueryAsync(
            @"UPDATE ReportDistributionLists
              SET IsActive = 0
              WHERE TenantId = @TenantId AND ListId = @ListId",
            new() { ["TenantId"] = tenantId, ["ListId"] = listId });

        return rowsAffected > 0;
    }
}

/// <summary>
/// Report distribution list
/// </summary>
public class ReportDistributionList
{
    public int ListId { get; set; }
    public int TenantId { get; set; }
    public int CreatedBy { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReportRecipient> Recipients { get; set; } = new();
}

/// <summary>
/// Report recipient in distribution list
/// </summary>
public class ReportRecipient
{
    public int RecipientId { get; set; }
    public int ListId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}
