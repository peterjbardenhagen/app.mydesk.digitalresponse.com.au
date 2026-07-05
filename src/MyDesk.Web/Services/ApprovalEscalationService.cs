using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyDesk.Web.Services;

/// <summary>
/// Escalation Worker: Routes approval requests to delegated approvers or escalates to next authority level
/// Part of Orchestrator-Worker Agentic Pattern (Phase 4)
/// </summary>
public class ApprovalEscalationService
{
    private readonly DatabaseService _db;
    private readonly ApprovalDelegationService _delegation;
    private readonly ComplianceAuditService _audit;
    private readonly NotificationService _notification;

    public ApprovalEscalationService(DatabaseService db, ApprovalDelegationService delegation,
        ComplianceAuditService audit, NotificationService notification)
    {
        _db = db;
        _delegation = delegation;
        _audit = audit;
        _notification = notification;
    }

    /// <summary>
    /// Resolve the actual approver considering delegations and escalations
    /// Returns list of approvers in priority order (primary, then delegates, then escalation path)
    /// </summary>
    public async Task<List<int>> ResolveApprovalChainAsync(int tenantId, int primaryApproverId,
        string moduleType, decimal? amount = null)
    {
        var chain = new List<int> { primaryApproverId };
        var delegates = new List<int>();

        // Get active delegations for primary approver
        var activeDelegates = await _delegation.GetActiveDelegatesAsync(tenantId, primaryApproverId, moduleType);

        // Filter by threshold if amount provided
        if (amount.HasValue)
        {
            foreach (var delegateId in activeDelegates)
            {
                var canApprove = await _delegation.CanApproveAsync(tenantId, delegateId, primaryApproverId, amount, moduleType);
                if (canApprove)
                    delegates.Add(delegateId);
            }
        }
        else
        {
            delegates.AddRange(activeDelegates);
        }

        // Add delegates to chain
        chain.AddRange(delegates);

        // If amount exceeds delegate thresholds, escalate to manager
        if (amount.HasValue && delegates.Count == 0 && activeDelegates.Count > 0)
        {
            var managerId = await GetTeamManagerAsync(tenantId, primaryApproverId);
            if (managerId.HasValue && managerId.Value != primaryApproverId)
                chain.Add(managerId.Value);
        }

        return chain;
    }

    /// <summary>
    /// Get the team manager for a user
    /// </summary>
    private async Task<int?> GetTeamManagerAsync(int tenantId, int userId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT DISTINCT t.TeamLeadUserId FROM TeamMembers tm
              JOIN Teams t ON tm.TeamId = t.TeamId
              WHERE tm.TenantId = @TenantId AND tm.UserId = @UserId AND tm.[Status] = 'Active'",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        if (dt.Rows.Count > 0 && dt.Rows[0]["TeamLeadUserId"] != DBNull.Value)
            return (int)dt.Rows[0]["TeamLeadUserId"];

        return null;
    }

    /// <summary>
    /// Route approval request considering delegations
    /// Returns (approver_id, is_delegated, delegation_id, notes)
    /// </summary>
    public async Task<ApprovalRouting> RouteApprovalAsync(int tenantId, int requestedApproverId,
        string moduleType, decimal? amount, int expenseId)
    {
        var chain = await ResolveApprovalChainAsync(tenantId, requestedApproverId, moduleType, amount);

        if (chain.Count == 0)
            throw new InvalidOperationException("No valid approvers found");

        // Find first available approver (not currently reviewing)
        int actualApproverId = chain[0];
        int? delegationId = null;
        bool isDelegated = false;

        if (chain.Count > 1)
        {
            // Check if primary is actually delegating or if amount-based escalation
            var delegation = await _delegation.GetDelegationAsync(tenantId, requestedApproverId, chain[1], moduleType);
            if (delegation != null)
            {
                actualApproverId = chain[1];
                isDelegated = true;
                delegationId = (int)delegation["DelegationId"];
            }
        }

        var notes = "";
        if (isDelegated)
            notes = $"Delegated from user {requestedApproverId} to {actualApproverId}";
        else if (actualApproverId != requestedApproverId)
            notes = $"Escalated to user {actualApproverId} (primary approver unavailable)";

        await _audit.LogAsync("ApprovalRouted", "ApprovalEscalation", new
        {
            tenantId,
            expenseId,
            requestedApproverId,
            actualApproverId,
            isDelegated,
            delegationId,
            moduleType,
            amount,
            notes
        });

        return new ApprovalRouting
        {
            ApproverId = actualApproverId,
            IsDelegated = isDelegated,
            DelegationId = delegationId,
            Notes = notes,
            ApprovalChain = chain
        };
    }

    /// <summary>
    /// Check if approver is available (no current approvals pending)
    /// </summary>
    public async Task<bool> IsApproverAvailableAsync(int tenantId, int userId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT COUNT(*) as PendingCount FROM ApprovalRequests
              WHERE TenantId = @TenantId AND CurrentApprover = @UserId AND [Status] = 'Pending'",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        int pending = (int)dt.Rows[0]["PendingCount"];
        return pending == 0;
    }

    /// <summary>
    /// Get all pending approvals for a user (including delegations)
    /// </summary>
    public async Task<DataTable> GetPendingApprovalsAsync(int tenantId, int userId, bool includeDelegated = true)
    {
        var query = @"SELECT ar.ApprovalRequestId, ar.TenantId, ar.ModuleType, ar.ModuleId,
                            ar.SubmittedById, ar.SubmittedAt, ar.[Status], ar.CurrentApprover,
                            ar.CurrentLevel, u.[Name] as SubmitterName
                     FROM ApprovalRequests ar
                     LEFT JOIN Users u ON ar.SubmittedById = u.UserId
                     WHERE ar.TenantId = @TenantId AND ar.[Status] = 'Pending'
                     AND (ar.CurrentApprover = @UserId";

        if (includeDelegated)
        {
            query += @" OR ar.CurrentApprover IN (
                SELECT FromUserId FROM ApprovalDelegation
                WHERE TenantId = @TenantId AND ToUserId = @UserId AND IsActive = 1
                AND StartDate <= CAST(GETUTCDATE() AS DATE)
                AND (EndDate IS NULL OR EndDate >= CAST(GETUTCDATE() AS DATE)))";
        }

        query += ") ORDER BY ar.SubmittedAt DESC";

        return await _db.QueryAsync(query, new() { ["TenantId"] = tenantId, ["UserId"] = userId });
    }

    /// <summary>
    /// Notify delegated approver when approval is routed to them
    /// </summary>
    public async Task NotifyDelegateAsync(int tenantId, int delegatedApproverId, int expenseId,
        int originalApproverId, string expenseDescription, decimal amount)
    {
        await _notification.SendNotificationAsync(
            tenantId,
            delegatedApproverId,
            "ApprovalDelegatedToYou",
            new Dictionary<string, object>
            {
                { "ExpenseId", expenseId },
                { "ExpenseDescription", expenseDescription },
                { "Amount", amount.ToString("C") },
                { "DelegatedFromUserId", originalApproverId }
            },
            "Approval",
            expenseId);
    }

    /// <summary>
    /// Notify escalation when approval is escalated beyond delegates
    /// </summary>
    public async Task NotifyEscalationAsync(int tenantId, int escalatedToApproverId, int expenseId,
        string expenseDescription, decimal amount, int originalApproverId)
    {
        await _notification.SendNotificationAsync(
            tenantId,
            escalatedToApproverId,
            "ApprovalEscalatedToYou",
            new Dictionary<string, object>
            {
                { "ExpenseId", expenseId },
                { "ExpenseDescription", expenseDescription },
                { "Amount", amount.ToString("C") },
                { "EscalatedFromUserId", originalApproverId }
            },
            "Approval",
            expenseId);
    }
}

public class ApprovalRouting
{
    public int ApproverId { get; set; }
    public bool IsDelegated { get; set; }
    public int? DelegationId { get; set; }
    public string Notes { get; set; } = "";
    public List<int> ApprovalChain { get; set; } = new();
}
