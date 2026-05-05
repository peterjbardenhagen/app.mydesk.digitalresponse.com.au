using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class AuditService
{
    private readonly DatabaseService _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(DatabaseService db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(string entityType, int entityId, string userCode, string action, string? details = null)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
                INSERT INTO EntityAudit (EntityType, EntityId, Code, Action, Details, Timestamp)
                VALUES (@Type, @Id, @User, @Action, @Details, GETDATE())",
                new()
                {
                    ["Type"] = entityType,
                    ["Id"] = entityId,
                    ["User"] = userCode,
                    ["Action"] = action,
                    ["Details"] = (object?)details ?? DBNull.Value
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit for {Type} #{Id}", entityType, entityId);
        }
    }

    public async Task<List<AuditEntry>> GetAuditTrailAsync(string entityType, int entityId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT a.*, u.Name AS UserName
            FROM EntityAudit a
            LEFT JOIN Users u ON a.Code = u.Code
            WHERE a.EntityType = @Type AND a.EntityId = @Id
            ORDER BY a.Timestamp DESC",
            new() { ["Type"] = entityType, ["Id"] = entityId });

        return dt.Map(r => new AuditEntry
        {
            AuditId = Convert.ToInt32(r["AuditId"]),
            EntityType = r["EntityType"]?.ToString() ?? "",
            EntityId = Convert.ToInt32(r["EntityId"]),
            UserCode = r["Code"]?.ToString() ?? "",
            UserName = r["UserName"]?.ToString() ?? r["Code"]?.ToString() ?? "System",
            Action = r["Action"]?.ToString() ?? "",
            Details = r["Details"]?.ToString(),
            Timestamp = Convert.ToDateTime(r["Timestamp"])
        });
    }
}

public class AuditEntry
{
    public int AuditId { get; set; }
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string UserCode { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
