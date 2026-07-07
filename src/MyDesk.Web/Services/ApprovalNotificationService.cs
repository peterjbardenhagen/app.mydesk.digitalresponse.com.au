using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for sending approval-specific notifications.
/// Handles delegated approvals, escalations, and approval reminders.
/// Part of Phase 5: Notifications & Alerts
/// </summary>
public class ApprovalNotificationService
{
    private readonly DatabaseService _db;
    private readonly NotificationService _notification;
    private readonly ApprovalDelegationService _delegation;
    private readonly ILogger<ApprovalNotificationService>? _logger;

    public ApprovalNotificationService(
        DatabaseService db,
        NotificationService notification,
        ApprovalDelegationService delegation,
        ILogger<ApprovalNotificationService>? logger = null)
    {
        _db = db;
        _notification = notification;
        _delegation = delegation;
        _logger = logger;
    }

    /// <summary>
    /// Send notification when approval is delegated to another user
    /// </summary>
    public async Task<bool> NotifyDelegateAsync(
        int tenantId,
        int approvalId,
        int delegateUserId,
        int delegatedByUserId,
        string reason)
    {
        try
        {
            _logger?.LogInformation(
                "Notifying delegate {DelegateUserId} of approval delegation for {ApprovalId}",
                delegateUserId, approvalId);

            // Get approval details
            var approvalResult = await _db.QueryAsync(
                @"SELECT A.ApprovalId, A.RequestorUserId, A.ExpenseId, A.Amount, A.Status,
                         E.Description, E.Amount as ExpenseAmount, E.Category,
                         U.Name as DelegatedByName, DU.Name as DelegateName
                  FROM dbo.Approvals A
                  JOIN dbo.Users U ON A.ApproverUserId = U.UserId
                  JOIN dbo.Users DU ON DU.UserId = @DelegateUserId
                  LEFT JOIN dbo.Expenses E ON A.ExpenseId = E.ExpenseId
                  WHERE A.TenantId = @TenantId AND A.ApprovalId = @ApprovalId",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["ApprovalId"] = approvalId,
                    ["DelegateUserId"] = delegateUserId
                });

            if (approvalResult.Rows.Count == 0)
            {
                _logger?.LogWarning("Approval {ApprovalId} not found", approvalId);
                return false;
            }

            var approval = approvalResult.Rows[0];
            decimal amount = (decimal)approval["Amount"];
            string description = approval["Description"]?.ToString() ?? "Expense Approval";
            string delegatedByName = approval["DelegatedByName"]?.ToString() ?? "Manager";
            string delegateName = approval["DelegateName"]?.ToString() ?? "Team Member";

            // Send notification
            var placeholders = new Dictionary<string, object>
            {
                { "DelegateName", delegateName },
                { "DelegatedByName", delegatedByName },
                { "ApprovalId", approvalId },
                { "Amount", amount.ToString("C") },
                { "Description", description },
                { "Reason", reason },
                { "DueDate", DateTime.UtcNow.AddDays(3).ToString("dd/MM/yyyy") }
            };

            await _notification.SendNotificationAsync(
                tenantId,
                delegateUserId,
                "ApprovalDelegated",
                placeholders,
                "Approval",
                approvalId,
                delegatedByUserId);

            // Log approval notification
            await LogApprovalNotificationAsync(
                tenantId,
                approvalId,
                "Delegated",
                delegateUserId,
                delegatedByUserId);

            _logger?.LogInformation(
                "Successfully notified delegate {DelegateUserId} about delegation",
                delegateUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error notifying delegate about approval delegation");
            return false;
        }
    }

    /// <summary>
    /// Send notification when approval is escalated to higher authority
    /// </summary>
    public async Task<bool> NotifyEscalationAsync(
        int tenantId,
        int approvalId,
        int escalatedToUserId,
        int escalatedByUserId,
        string escalationReason)
    {
        try
        {
            _logger?.LogInformation(
                "Notifying escalation recipient {RecipientUserId} for approval {ApprovalId}",
                escalatedToUserId, approvalId);

            // Get approval and escalation details
            var approvalResult = await _db.QueryAsync(
                @"SELECT A.ApprovalId, A.RequestorUserId, A.ExpenseId, A.Amount, A.Status,
                         E.Description, E.Category, E.CreatedAt,
                         U.Name as EscalatedByName, EU.Name as EscalatedToName,
                         RU.Name as RequestorName
                  FROM dbo.Approvals A
                  JOIN dbo.Users U ON A.ApproverUserId = U.UserId
                  JOIN dbo.Users EU ON EU.UserId = @EscalatedToUserId
                  JOIN dbo.Users RU ON RU.UserId = A.RequestorUserId
                  LEFT JOIN dbo.Expenses E ON A.ExpenseId = E.ExpenseId
                  WHERE A.TenantId = @TenantId AND A.ApprovalId = @ApprovalId",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["ApprovalId"] = approvalId,
                    ["EscalatedToUserId"] = escalatedToUserId
                });

            if (approvalResult.Rows.Count == 0)
            {
                _logger?.LogWarning("Approval {ApprovalId} not found for escalation", approvalId);
                return false;
            }

            var approval = approvalResult.Rows[0];
            decimal amount = (decimal)approval["Amount"];
            string description = approval["Description"]?.ToString() ?? "Expense Approval";
            string escalatedByName = approval["EscalatedByName"]?.ToString() ?? "Manager";
            string escalatedToName = approval["EscalatedToName"]?.ToString() ?? "Director";
            string requestorName = approval["RequestorName"]?.ToString() ?? "Employee";

            // Send notification
            var placeholders = new Dictionary<string, object>
            {
                { "EscalatedToName", escalatedToName },
                { "EscalatedByName", escalatedByName },
                { "RequestorName", requestorName },
                { "ApprovalId", approvalId },
                { "Amount", amount.ToString("C") },
                { "Description", description },
                { "EscalationReason", escalationReason },
                { "Priority", amount > 10000 ? "High" : "Normal" }
            };

            await _notification.SendNotificationAsync(
                tenantId,
                escalatedToUserId,
                "ApprovalEscalated",
                placeholders,
                "Approval",
                approvalId,
                escalatedByUserId);

            // Log approval notification
            await LogApprovalNotificationAsync(
                tenantId,
                approvalId,
                "Escalated",
                escalatedToUserId,
                escalatedByUserId);

            _logger?.LogInformation(
                "Successfully notified escalation recipient about escalation",
                escalatedToUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error notifying escalation recipient");
            return false;
        }
    }

    /// <summary>
    /// Send approval reminder notifications to pending approvers
    /// </summary>
    public async Task<int> SendApprovalRemindersAsync(int tenantId, int daysOld = 3)
    {
        try
        {
            _logger?.LogInformation(
                "Sending approval reminders for approvals pending > {DaysOld} days",
                daysOld);

            // Get pending approvals older than threshold
            var pendingApprovals = await _db.QueryAsync(
                @"SELECT A.ApprovalId, A.ApproverUserId, A.RequestorUserId, A.Amount,
                         A.CreatedAt, E.Description,
                         U.Name as ApproverName, RU.Name as RequestorName
                  FROM dbo.Approvals A
                  JOIN dbo.Users U ON A.ApproverUserId = U.UserId
                  JOIN dbo.Users RU ON RU.UserId = A.RequestorUserId
                  LEFT JOIN dbo.Expenses E ON A.ExpenseId = E.ExpenseId
                  WHERE A.TenantId = @TenantId AND A.Status = 'Pending'
                        AND DATEDIFF(DAY, A.CreatedAt, GETUTCDATE()) >= @Days",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Days"] = daysOld
                });

            int reminders = 0;

            foreach (DataRow approval in pendingApprovals.Rows)
            {
                int approvalId = (int)approval["ApprovalId"];
                int approverId = (int)approval["ApproverUserId"];
                int requestorId = (int)approval["RequestorUserId"];
                decimal amount = (decimal)approval["Amount"];
                DateTime createdAt = (DateTime)approval["CreatedAt"];
                string description = approval["Description"]?.ToString() ?? "Expense";
                string approverName = approval["ApproverName"]?.ToString() ?? "Approver";
                string requestorName = approval["RequestorName"]?.ToString() ?? "Employee";

                int daysPending = (int)(DateTime.UtcNow - createdAt).TotalDays;

                var placeholders = new Dictionary<string, object>
                {
                    { "ApproverName", approverName },
                    { "RequestorName", requestorName },
                    { "Amount", amount.ToString("C") },
                    { "Description", description },
                    { "DaysPending", daysPending },
                    { "ApprovalId", approvalId }
                };

                // Send reminder
                await _notification.SendNotificationAsync(
                    tenantId,
                    approverId,
                    "ApprovalReminder",
                    placeholders,
                    "Approval",
                    approvalId);

                // Log notification
                await LogApprovalNotificationAsync(
                    tenantId,
                    approvalId,
                    "Reminder",
                    approverId,
                    null);

                reminders++;
                _logger?.LogInformation(
                    "Sent reminder for approval {ApprovalId} pending {Days} days",
                    approvalId, daysPending);
            }

            return reminders;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending approval reminders");
            return 0;
        }
    }

    /// <summary>
    /// Send notification when approval is approved/rejected
    /// </summary>
    public async Task<bool> NotifyApprovalDecisionAsync(
        int tenantId,
        int approvalId,
        int requestorUserId,
        string decision,
        string comments,
        int approverUserId)
    {
        try
        {
            _logger?.LogInformation(
                "Notifying requestor {RequestorUserId} of approval decision: {Decision}",
                requestorUserId, decision);

            // Get approval details
            var approvalResult = await _db.QueryAsync(
                @"SELECT A.ApprovalId, A.Amount, A.Status,
                         E.Description, E.Category,
                         U.Name as ApproverName, RU.Name as RequestorName
                  FROM dbo.Approvals A
                  JOIN dbo.Users U ON A.ApproverUserId = U.UserId
                  JOIN dbo.Users RU ON RU.UserId = @RequestorUserId
                  LEFT JOIN dbo.Expenses E ON A.ExpenseId = E.ExpenseId
                  WHERE A.TenantId = @TenantId AND A.ApprovalId = @ApprovalId",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["ApprovalId"] = approvalId,
                    ["RequestorUserId"] = requestorUserId
                });

            if (approvalResult.Rows.Count == 0)
            {
                _logger?.LogWarning("Approval {ApprovalId} not found", approvalId);
                return false;
            }

            var approval = approvalResult.Rows[0];
            decimal amount = (decimal)approval["Amount"];
            string description = approval["Description"]?.ToString() ?? "Expense";
            string approverName = approval["ApproverName"]?.ToString() ?? "Manager";
            string requestorName = approval["RequestorName"]?.ToString() ?? "Employee";

            // Determine event type based on decision
            string eventType = decision.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                ? "ApprovalApproved"
                : "ApprovalRejected";

            var placeholders = new Dictionary<string, object>
            {
                { "RequestorName", requestorName },
                { "ApproverName", approverName },
                { "ApprovalId", approvalId },
                { "Amount", amount.ToString("C") },
                { "Description", description },
                { "Decision", decision },
                { "Comments", comments ?? "No comments" }
            };

            await _notification.SendNotificationAsync(
                tenantId,
                requestorUserId,
                eventType,
                placeholders,
                "Approval",
                approvalId,
                approverUserId);

            // Log approval notification
            await LogApprovalNotificationAsync(
                tenantId,
                approvalId,
                decision.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : "Rejected",
                requestorUserId,
                approverUserId);

            _logger?.LogInformation(
                "Successfully notified requestor of approval decision: {Decision}",
                decision);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error notifying requestor of approval decision");
            return false;
        }
    }

    /// <summary>
    /// Log approval notification to ApprovalNotifications table
    /// </summary>
    private async Task LogApprovalNotificationAsync(
        int tenantId,
        int approvalId,
        string eventType,
        int recipientUserId,
        int? triggeredByUserId)
    {
        try
        {
            await _db.ExecuteAsync(
                @"INSERT INTO dbo.ApprovalNotifications
                  (TenantId, ApprovalId, EventType, RecipientUserId, TriggeredByUserId, Status, CreatedAt)
                  VALUES (@TenantId, @ApprovalId, @EventType, @RecipientUserId, @TriggeredBy, 'Sent', GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["ApprovalId"] = approvalId,
                    ["EventType"] = eventType,
                    ["RecipientUserId"] = recipientUserId,
                    ["TriggeredBy"] = triggeredByUserId ?? (object)DBNull.Value
                });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging approval notification");
        }
    }

    /// <summary>
    /// Get notification history for an approval
    /// </summary>
    public async Task<DataTable> GetApprovalNotificationHistoryAsync(
        int tenantId,
        int approvalId)
    {
        return await _db.QueryAsync(
            @"SELECT NotificationId, EventType, RecipientUserId, TriggeredByUserId,
                     Status, SentAt, DeliveredAt, CreatedAt
              FROM dbo.ApprovalNotifications
              WHERE TenantId = @TenantId AND ApprovalId = @ApprovalId
              ORDER BY CreatedAt DESC",
            new()
            {
                ["TenantId"] = tenantId,
                ["ApprovalId"] = approvalId
            });
    }
}
