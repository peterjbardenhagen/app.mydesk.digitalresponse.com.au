using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDesk.Web.Services;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Notification Controller: Manage user notifications, preferences, and delivery status
/// Part of Phase 5: Notifications & Alerts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _notification;
    private readonly DatabaseService _db;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        NotificationService notification,
        DatabaseService db,
        ILogger<NotificationController> logger)
    {
        _notification = notification;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get unread notifications for current user
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications(int tenantId, int limit = 10)
    {
        try
        {
            _logger.LogInformation("Getting unread notifications for tenant {TenantId}", tenantId);

            var (notifications, unreadCount) = await _notification.GetUnreadNotificationsAsync(
                tenantId, userId: User.FindFirst("sub")?.Value ?? "0", limit);

            return Ok(new
            {
                notifications,
                unreadCount,
                limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notifications");
            return BadRequest(new { error = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{notificationId}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        try
        {
            _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);

            await _notification.MarkAsReadAsync(notificationId);

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return BadRequest(new { error = "Failed to update notification" });
        }
    }

    /// <summary>
    /// Mark all notifications as read for user
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(int tenantId)
    {
        try
        {
            _logger.LogInformation("Marking all notifications as read for tenant {TenantId}", tenantId);

            var userId = int.TryParse(User.FindFirst("sub")?.Value ?? "0", out var id) ? id : 0;

            await _notification.MarkAllAsReadAsync(tenantId, userId);

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return BadRequest(new { error = "Failed to update notifications" });
        }
    }

    /// <summary>
    /// Get notification delivery status and history
    /// </summary>
    [HttpGet("delivery-status")]
    public async Task<IActionResult> GetDeliveryStatus(int tenantId, int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting notification delivery status for tenant {TenantId}", tenantId);

            var result = await _db.QueryAsync(
                @"SELECT TOP (@Limit)
                    NotificationId, EventType, NotificationType, Status, CreatedAt, SentAt, FailedAt, ErrorMessage,
                    RecipientEmail, Subject
                  FROM dbo.NotificationLog
                  WHERE TenantId = @TenantId
                  ORDER BY CreatedAt DESC",
                new() { ["TenantId"] = tenantId, ["Limit"] = limit });

            var statusList = new List<object>();
            foreach (DataRow row in result.Rows)
            {
                statusList.Add(new
                {
                    notificationId = row["NotificationId"],
                    eventType = row["EventType"],
                    notificationType = row["NotificationType"],
                    status = row["Status"],
                    recipientEmail = row["RecipientEmail"],
                    subject = row["Subject"],
                    createdAt = row["CreatedAt"],
                    sentAt = row["SentAt"] != DBNull.Value ? row["SentAt"] : null,
                    failedAt = row["FailedAt"] != DBNull.Value ? row["FailedAt"] : null,
                    errorMessage = row["ErrorMessage"]
                });
            }

            return Ok(statusList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery status");
            return BadRequest(new { error = "Failed to retrieve delivery status" });
        }
    }

    /// <summary>
    /// Get email queue status
    /// </summary>
    [HttpGet("email-queue")]
    public async Task<IActionResult> GetEmailQueueStatus(int tenantId)
    {
        try
        {
            _logger.LogInformation("Getting email queue status for tenant {TenantId}", tenantId);

            var pendingResult = await _db.QueryAsync(
                @"SELECT COUNT(*) as PendingCount FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId AND Status = 'Pending'",
                new() { ["TenantId"] = tenantId });

            var sentResult = await _db.QueryAsync(
                @"SELECT COUNT(*) as SentCount FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId AND Status = 'Sent'",
                new() { ["TenantId"] = tenantId });

            var failedResult = await _db.QueryAsync(
                @"SELECT COUNT(*) as FailedCount FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId AND Status = 'Failed'",
                new() { ["TenantId"] = tenantId });

            int pending = pendingResult.Rows.Count > 0 ? (int)pendingResult.Rows[0]["PendingCount"] : 0;
            int sent = sentResult.Rows.Count > 0 ? (int)sentResult.Rows[0]["SentCount"] : 0;
            int failed = failedResult.Rows.Count > 0 ? (int)failedResult.Rows[0]["FailedCount"] : 0;

            return Ok(new { pending, sent, failed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email queue status");
            return BadRequest(new { error = "Failed to retrieve queue status" });
        }
    }

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(int tenantId, int userId)
    {
        try
        {
            _logger.LogInformation("Getting notification preferences for user {UserId}", userId);

            var result = await _db.QueryAsync(
                @"SELECT EnableEmailNotifications, EmailOnApprovalRequired, EmailDigestFrequency,
                         EnableSmsNotifications, PhoneNumber, EnableInAppNotifications,
                         QuietHoursEnabled, QuietHoursStart, QuietHoursEnd
                  FROM dbo.NotificationSettings
                  WHERE TenantId = @TenantId AND UserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });

            if (result.Rows.Count == 0)
                return NotFound("Preferences not found");

            var row = result.Rows[0];
            return Ok(new
            {
                enableEmailNotifications = (bool)row["EnableEmailNotifications"],
                emailOnApprovalRequired = (bool)row["EmailOnApprovalRequired"],
                emailDigestFrequency = row["EmailDigestFrequency"].ToString(),
                enableSmsNotifications = (bool)row["EnableSmsNotifications"],
                phoneNumber = row["PhoneNumber"].ToString(),
                enableInAppNotifications = (bool)row["EnableInAppNotifications"],
                quietHoursEnabled = (bool)row["QuietHoursEnabled"],
                quietHoursStart = row["QuietHoursStart"] != DBNull.Value ? row["QuietHoursStart"] : null,
                quietHoursEnd = row["QuietHoursEnd"] != DBNull.Value ? row["QuietHoursEnd"] : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences");
            return BadRequest(new { error = "Failed to retrieve preferences" });
        }
    }

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    [HttpPost("preferences")]
    public async Task<IActionResult> UpdatePreferences(int tenantId, int userId, [FromBody] NotificationPreferencesRequest request)
    {
        try
        {
            _logger.LogInformation("Updating notification preferences for user {UserId}", userId);

            var preferences = new NotificationPreferences
            {
                EnableEmailNotifications = request.EnableEmailNotifications,
                EmailOnApprovalRequired = request.EmailOnApprovalRequired,
                EmailDigestFrequency = request.EmailDigestFrequency,
                EnableSmsNotifications = request.EnableSmsNotifications,
                PhoneNumber = request.PhoneNumber,
                EnableInAppNotifications = request.EnableInAppNotifications,
                QuietHoursEnabled = request.QuietHoursEnabled,
                QuietHoursStart = request.QuietHoursStart,
                QuietHoursEnd = request.QuietHoursEnd
            };

            await _notification.UpdatePreferencesAsync(tenantId, userId, preferences);

            return Ok(new { message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences");
            return BadRequest(new { error = "Failed to update preferences" });
        }
    }

    /// <summary>
    /// Retry failed notifications
    /// </summary>
    [HttpPost("retry-failed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RetryFailedNotifications(int tenantId, int maxRetries = 3)
    {
        try
        {
            _logger.LogInformation("Retrying failed notifications for tenant {TenantId}", tenantId);

            var result = await _db.QueryAsync(
                @"SELECT TOP 100 EmailQueueId, ToEmail, Subject, BodyHtml, NotificationLogId, RetryCount
                  FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId AND Status = 'Failed' AND RetryCount < @MaxRetries
                  ORDER BY CreatedAt ASC",
                new() { ["TenantId"] = tenantId, ["MaxRetries"] = maxRetries });

            int retryCount = 0;
            foreach (DataRow row in result.Rows)
            {
                await _db.ExecuteNonQueryAsync(
                    @"UPDATE dbo.EmailQueue
                      SET Status = 'Pending', RetryCount = RetryCount + 1, UpdatedAt = GETUTCDATE()
                      WHERE EmailQueueId = @Id",
                    new() { ["Id"] = row["EmailQueueId"] });

                retryCount++;
            }

            _logger.LogInformation("Queued {Count} notifications for retry", retryCount);
            return Ok(new { retriedCount = retryCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed notifications");
            return BadRequest(new { error = "Failed to retry notifications" });
        }
    }

    /// <summary>
    /// Get notification audit trail for entity
    /// </summary>
    [HttpGet("audit/{entityType}/{entityId}")]
    public async Task<IActionResult> GetAuditTrail(int tenantId, string entityType, int entityId, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Getting notification audit trail for {EntityType} {EntityId}", entityType, entityId);

            var result = await _db.QueryAsync(
                @"SELECT TOP (@Limit)
                    NotificationId, EventType, NotificationType, RecipientEmail, Status, CreatedAt,
                    SentAt, FailedAt, Subject, TriggeredByUserId
                  FROM dbo.NotificationLog
                  WHERE TenantId = @TenantId AND EventEntityType = @EntityType AND EventEntityId = @EntityId
                  ORDER BY CreatedAt DESC",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["EntityType"] = entityType,
                    ["EntityId"] = entityId,
                    ["Limit"] = limit
                });

            var auditList = new List<object>();
            foreach (DataRow row in result.Rows)
            {
                auditList.Add(new
                {
                    notificationId = row["NotificationId"],
                    eventType = row["EventType"],
                    notificationType = row["NotificationType"],
                    recipientEmail = row["RecipientEmail"],
                    status = row["Status"],
                    subject = row["Subject"],
                    createdAt = row["CreatedAt"],
                    sentAt = row["SentAt"] != DBNull.Value ? row["SentAt"] : null,
                    triggeredBy = row["TriggeredByUserId"] != DBNull.Value ? (int)row["TriggeredByUserId"] : (int?)null
                });
            }

            return Ok(auditList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail");
            return BadRequest(new { error = "Failed to retrieve audit trail" });
        }
    }
}

public class NotificationPreferencesRequest
{
    public bool EnableEmailNotifications { get; set; }
    public bool EmailOnApprovalRequired { get; set; }
    public string EmailDigestFrequency { get; set; } = "Immediate";
    public bool EnableSmsNotifications { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EnableInAppNotifications { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}
