using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Unified user-action log. Writes to UserActivity table (auto-created on first use).
/// Falls back to synthetic activity from QuoteAudit / Invoices / POs / Despatch when
/// the table is empty (i.e. before any explicit actions have been logged).
/// </summary>
public class ActivityService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(DatabaseService db, ILogger<ActivityService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        try
        {
            await _db.ExecuteAsync(@"
                IF OBJECT_ID(N'UserActivity', N'U') IS NULL
                BEGIN
                    CREATE TABLE UserActivity (
                        ActivityId   INT IDENTITY(1,1) PRIMARY KEY,
                        UserCode     NVARCHAR(20)  NOT NULL,
                        EntityType   NVARCHAR(50)  NOT NULL,
                        EntityId     INT           NULL,
                        EntityRef    NVARCHAR(200) NULL,
                        Action       NVARCHAR(500) NOT NULL,
                        ActivityDate DATETIME      NOT NULL DEFAULT GETDATE()
                    );
                    CREATE INDEX IX_UserActivity_Date ON UserActivity (ActivityDate DESC);
                    CREATE INDEX IX_UserActivity_User ON UserActivity (UserCode, ActivityDate DESC);
                END");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure UserActivity table");
        }
    }

    public async Task LogAsync(
        string userCode, string entityType, int? entityId,
        string action, string? entityRef = null)
    {
        if (string.IsNullOrWhiteSpace(userCode)) return;
        try
        {
            // Log to new EntityAudit table (Unified Audit)
            await _db.ExecuteAsync(
                @"INSERT INTO EntityAudit (EntityType, EntityId, Code, Action, Details, Timestamp)
                  VALUES (@Type, @EId, @Code, @Action, @Ref, GETDATE())",
                new()
                {
                    ["Code"]   = userCode,
                    ["Type"]   = entityType,
                    ["EId"]    = (object?)entityId ?? 0,
                    ["Ref"]    = (object?)entityRef ?? DBNull.Value,
                    ["Action"] = action,
                });

            // Keep legacy UserActivity for now to avoid breaking existing queries
            await _db.ExecuteAsync(
                @"INSERT INTO UserActivity (UserCode, EntityType, EntityId, EntityRef, Action, ActivityDate)
                  VALUES (@Code, @Type, @EId, @Ref, @Action, GETDATE())",
                new()
                {
                    ["Code"]   = userCode,
                    ["Type"]   = entityType,
                    ["EId"]    = (object?)entityId ?? DBNull.Value,
                    ["Ref"]    = (object?)entityRef ?? DBNull.Value,
                    ["Action"] = action,
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not log activity {Action}", action);
        }
    }

    public async Task<List<ActivityFeedItem>> GetRecentAsync(int limit = 30, string? userCode = null)
    {
        // Try explicit log first
        try
        {
            var sql = $@"SELECT TOP {limit}
                       ua.UserCode,
                       ISNULL(u.Name, ua.UserCode) AS UserName,
                       ua.EntityType, ua.EntityId,
                       ISNULL(ua.EntityRef, N'') AS EntityRef,
                       ua.Action, ua.ActivityDate
                   FROM UserActivity ua
                   LEFT JOIN Users u ON u.Code = ua.UserCode";
            
            var p = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(userCode))
            {
                sql += " WHERE ua.UserCode = @UserCode";
                p["UserCode"] = userCode;
            }
            
            sql += " ORDER BY ua.ActivityDate DESC";

            var dt = await _db.QueryAsync(sql, p);

            if (dt.Rows.Count > 0)
                return dt.Map(MapItem).ToList();
        }
        catch { }

        return await GetSyntheticAsync(limit, userCode);
    }

    private async Task<List<ActivityFeedItem>> GetSyntheticAsync(int limit, string? userCode = null)
    {
        var items = new List<ActivityFeedItem>();

        var userFilter = !string.IsNullOrEmpty(userCode) ? $" AND qa.Code = @UserCode" : "";
        var p = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(userCode)) p["UserCode"] = userCode;

        // QuoteAudit — richest source, has real action descriptions
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP 25
                    qa.Code AS UserCode,
                    ISNULL(u.Name, qa.Code) AS UserName,
                    N'Quote' AS EntityType, qa.Qid AS EntityId,
                    ISNULL(q.Reference, CONCAT(N'#', CAST(qa.Qid AS NVARCHAR(10)))) AS EntityRef,
                    qa.Action, qa.DateEntered AS ActivityDate
                FROM QuoteAudit qa
                LEFT JOIN Users u ON u.Code = qa.Code
                LEFT JOIN Quotes q ON q.Qid = qa.Qid
                WHERE 1=1{userFilter}
                ORDER BY qa.DateEntered DESC", p);
            items.AddRange(dt.Map(MapItem));
        }
        catch { }

        // Recent Invoices
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP 10
                    ISNULL(i.Code, N'') AS UserCode,
                    ISNULL(u.Name, N'') AS UserName,
                    N'Invoice' AS EntityType, i.InvoiceId AS EntityId,
                    CONCAT(N'#', CAST(i.InvoiceId AS NVARCHAR(10))) AS EntityRef,
                    CONCAT(N'Invoice — ', ISNULL(s.InvoiceStatus, N'created')) AS Action,
                    i.InvoiceDate AS ActivityDate
                FROM Invoices i
                LEFT JOIN Users u ON u.Code = i.Code
                LEFT JOIN InvoiceStatus s ON s.InvoiceStatusId = i.InvoiceStatusId
                WHERE 1=1{userFilter}
                ORDER BY i.InvoiceDate DESC", p);
            items.AddRange(dt.Map(MapItem));
        }
        catch { }

        // Recent Purchase Orders
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP 10
                    ISNULL(p.Code, N'') AS UserCode,
                    ISNULL(u.Name, N'') AS UserName,
                    N'PO' AS EntityType, p.POid AS EntityId,
                    CONCAT(N'PO#', CAST(p.POid AS NVARCHAR(10))) AS EntityRef,
                    CONCAT(N'PO — ', ISNULL(ps.POStatus, N'raised')) AS Action,
                    p.PODate AS ActivityDate
                FROM PurchaseOrders p
                LEFT JOIN Users u ON u.Code = p.Code
                LEFT JOIN PurchaseOrderStatus ps ON ps.POStatusId = p.POStatusId
                WHERE 1=1{userFilter}
                ORDER BY p.PODate DESC", p);
            items.AddRange(dt.Map(MapItem));
        }
        catch { }

        // Recent Despatch
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 10
                    N'' AS UserCode, N'' AS UserName,
                    N'Despatch' AS EntityType, d.DespatchId AS EntityId,
                    CONCAT(N'Despatch #', CAST(d.DespatchId AS NVARCHAR(10))) AS EntityRef,
                    CONCAT(N'Item despatched',
                           CASE WHEN co.Company IS NOT NULL THEN CONCAT(N' — ', co.Company) ELSE N'' END) AS Action,
                    ISNULL(d.DespatchDate, CAST(d.DespatchId AS DATETIME)) AS ActivityDate
                FROM Despatch d
                LEFT JOIN Invoices  i  ON i.InvoiceId = d.InvoiceId
                LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
                ORDER BY d.DespatchId DESC");
            items.AddRange(dt.Map(MapItem));
        }
        catch { }

        return items
            .Where(i => i.ActivityDate > DateTime.MinValue)
            .OrderByDescending(i => i.ActivityDate)
            .Take(limit)
            .ToList();
    }

    private static ActivityFeedItem MapItem(DataRow r) => new()
    {
        UserCode     = r["UserCode"]?.ToString() ?? "",
        UserName     = r.Table.Columns.Contains("UserName") ? r["UserName"]?.ToString() ?? "" : "",
        EntityType   = r["EntityType"]?.ToString() ?? "",
        EntityId     = r["EntityId"] == DBNull.Value ? null : Convert.ToInt32(r["EntityId"]),
        EntityRef    = r["EntityRef"]?.ToString() ?? "",
        Action       = r["Action"]?.ToString() ?? "",
        ActivityDate = r["ActivityDate"] == DBNull.Value
                       ? DateTime.MinValue
                       : Convert.ToDateTime(r["ActivityDate"]),
    };
}
