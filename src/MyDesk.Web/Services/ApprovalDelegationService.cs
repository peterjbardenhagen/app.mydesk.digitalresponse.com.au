using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

public class ApprovalDelegationService
{
    private readonly DatabaseService _db;
    private readonly ComplianceAuditService _audit;

    public ApprovalDelegationService(DatabaseService db, ComplianceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<int> CreateDelegationAsync(int tenantId, int fromUserId, int toUserId,
        int? teamId = null, string? moduleType = null, decimal? minThreshold = null,
        decimal? maxThreshold = null, DateTime? startDate = null, DateTime? endDate = null,
        bool canApprove = true, bool canReject = true, bool canDelegate = false, bool canComment = true)
    {
        if (fromUserId == toUserId)
            throw new ArgumentException("Cannot delegate to self");

        startDate ??= DateTime.UtcNow.Date;

        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO ApprovalDelegation (TenantId, TeamId, FromUserId, ToUserId, ModuleType,
                                             MinThreshold, MaxThreshold, StartDate, EndDate,
                                             CanApprove, CanReject, CanDelegate, CanComment)
              VALUES (@TenantId, @TeamId, @FromId, @ToId, @ModuleType, @MinThresh, @MaxThresh,
                      @StartDate, @EndDate, @CanApprove, @CanReject, @CanDelegate, @CanComment)",
            new()
            {
                ["TenantId"] = tenantId,
                ["TeamId"] = (object?)teamId ?? DBNull.Value,
                ["FromId"] = fromUserId,
                ["ToId"] = toUserId,
                ["ModuleType"] = (object?)moduleType ?? DBNull.Value,
                ["MinThresh"] = (object?)minThreshold ?? DBNull.Value,
                ["MaxThresh"] = (object?)maxThreshold ?? DBNull.Value,
                ["StartDate"] = startDate.Value,
                ["EndDate"] = (object?)endDate ?? DBNull.Value,
                ["CanApprove"] = canApprove ? 1 : 0,
                ["CanReject"] = canReject ? 1 : 0,
                ["CanDelegate"] = canDelegate ? 1 : 0,
                ["CanComment"] = canComment ? 1 : 0
            });

        var dtId = await _db.QueryAsync(
            "SELECT MAX(DelegationId) as Id FROM ApprovalDelegation WHERE TenantId = @TenantId",
            new() { ["TenantId"] = tenantId });

        int delegationId = (int)dtId.Rows[0]["Id"];

        await _audit.LogAsync("DelegationCreated", "System", new
        {
            tenantId,
            delegationId,
            fromUserId,
            toUserId,
            teamId,
            moduleType,
            startDate,
            endDate
        });

        return delegationId;
    }

    public async Task<List<int>> GetActiveDelegatesAsync(int tenantId, int userId, string? moduleType = null)
    {
        var query = @"SELECT DISTINCT ToUserId FROM ApprovalDelegation
                     WHERE TenantId = @TenantId AND FromUserId = @UserId AND IsActive = 1
                     AND StartDate <= CAST(GETUTCDATE() AS DATE)
                     AND (EndDate IS NULL OR EndDate >= CAST(GETUTCDATE() AS DATE))";

        var parms = new Dictionary<string, object>
        {
            ["TenantId"] = tenantId,
            ["UserId"] = userId
        };

        if (!string.IsNullOrWhiteSpace(moduleType))
        {
            query += " AND (ModuleType IS NULL OR ModuleType = @ModuleType)";
            parms["ModuleType"] = moduleType;
        }

        var dt = await _db.QueryAsync(query, parms);

        return dt.Rows.Cast<DataRow>().Select(r => (int)r["ToUserId"]).ToList();
    }

    public async Task<DataRow?> GetDelegationAsync(int tenantId, int fromUserId, int toUserId,
        string? moduleType = null)
    {
        var query = @"SELECT DelegationId, TenantId, TeamId, FromUserId, ToUserId, ModuleType,
                            MinThreshold, MaxThreshold, StartDate, EndDate, CanApprove, CanReject,
                            CanDelegate, CanComment, IsActive, CreatedAt, UpdatedAt
                     FROM ApprovalDelegation
                     WHERE TenantId = @TenantId AND FromUserId = @FromId AND ToUserId = @ToId
                     AND StartDate <= CAST(GETUTCDATE() AS DATE)
                     AND (EndDate IS NULL OR EndDate >= CAST(GETUTCDATE() AS DATE))
                     AND IsActive = 1";

        var parms = new Dictionary<string, object>
        {
            ["TenantId"] = tenantId,
            ["FromId"] = fromUserId,
            ["ToId"] = toUserId
        };

        if (!string.IsNullOrWhiteSpace(moduleType))
        {
            query += " AND (ModuleType IS NULL OR ModuleType = @ModuleType)";
            parms["ModuleType"] = moduleType;
        }

        var dt = await _db.QueryAsync(query, parms);
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public async Task<bool> CanApproveAsync(int tenantId, int delegateUserId, int fromUserId,
        decimal? amount = null, string? moduleType = null)
    {
        var delegation = await GetDelegationAsync(tenantId, fromUserId, delegateUserId, moduleType);
        if (delegation == null)
            return false;

        bool canApprove = delegation["CanApprove"] != DBNull.Value ? (bool)delegation["CanApprove"] : false;
        if (!canApprove)
            return false;

        if (amount.HasValue)
        {
            var minThreshold = delegation["MinThreshold"];
            var maxThreshold = delegation["MaxThreshold"];

            if (minThreshold != DBNull.Value && amount < (decimal)minThreshold)
                return false;

            if (maxThreshold != DBNull.Value && amount > (decimal)maxThreshold)
                return false;
        }

        return true;
    }

    public async Task<DataTable> GetUserDelegationsAsync(int tenantId, int userId, bool asDelegator = true)
    {
        var column = asDelegator ? "FromUserId" : "ToUserId";
        var otherColumn = asDelegator ? "ToUserId" : "FromUserId";

        return await _db.QueryAsync(
            $@"SELECT ad.DelegationId, ad.TeamId, ad.{column}, ad.{otherColumn}, ad.ModuleType,
                      ad.MinThreshold, ad.MaxThreshold, ad.StartDate, ad.EndDate,
                      ad.CanApprove, ad.CanReject, ad.CanDelegate, ad.CanComment,
                      u.[Name] as OtherUserName, u.Email as OtherUserEmail
               FROM ApprovalDelegation ad
               LEFT JOIN Users u ON u.UserId = ad.{otherColumn}
               WHERE ad.TenantId = @TenantId AND ad.{column} = @UserId AND ad.IsActive = 1
               AND ad.StartDate <= CAST(GETUTCDATE() AS DATE)
               AND (ad.EndDate IS NULL OR ad.EndDate >= CAST(GETUTCDATE() AS DATE))
               ORDER BY ad.StartDate DESC",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });
    }

    public async Task DeactivateDelegationAsync(int tenantId, int delegationId)
    {
        await _db.ExecuteNonQueryAsync(
            @"UPDATE ApprovalDelegation
               SET IsActive = 0, UpdatedAt = GETUTCDATE()
               WHERE TenantId = @TenantId AND DelegationId = @DelegationId",
            new()
            {
                ["TenantId"] = tenantId,
                ["DelegationId"] = delegationId
            });

        await _audit.LogAsync("DelegationDeactivated", "System", new
        {
            tenantId,
            delegationId
        });
    }

    public async Task<List<int>> ResolveApprovalChainAsync(int tenantId, int userId, string? moduleType = null)
    {
        var chain = new List<int> { userId };
        var visited = new HashSet<int> { userId };
        var toProcess = new Queue<int>(new[] { userId });

        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();
            var delegates = await GetActiveDelegatesAsync(tenantId, current, moduleType);

            foreach (var delegateId in delegates)
            {
                if (visited.Contains(delegateId))
                    continue;

                visited.Add(delegateId);
                chain.Add(delegateId);
                toProcess.Enqueue(delegateId);
            }
        }

        return chain;
    }
}
