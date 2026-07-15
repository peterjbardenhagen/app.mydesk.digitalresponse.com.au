using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Notification Audit Service: Log all notification events for compliance and debugging
/// Part of Phase 5: Notifications & Alerts
/// </summary>
public class NotificationAuditService
{
    private readonly DatabaseService _db;
    private readonly ILogger<NotificationAuditService>? _logger;

    public NotificationAuditService(
        DatabaseService db,
        ILogger<NotificationAuditService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Log notification send event
    /// </summary>
    public async Task<bool> LogNotificationSentAsync(
        int tenantId,
        int notificationId,
        int recipientUserId,
        string eventType,
        string notificationType,
        string recipientEmail,
        string? entityType = null,
        int? entityId = null,
        int? triggeredByUserId = null)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationAuditLog
                  (TenantId, NotificationId, RecipientUserId, EventType, NotificationType, RecipientEmail,
                   EntityType, EntityId, TriggeredByUserId, Action, Timestamp)
                  VALUES (@TenantId, @NotificationId, @RecipientUserId, @EventType, @NotificationType,
                          @RecipientEmail, @EntityType, @EntityId, @TriggeredByUserId, 'Sent', GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["NotificationId"] = notificationId,
                    ["RecipientUserId"] = recipientUserId,
                    ["EventType"] = eventType,
                    ["NotificationType"] = notificationType,
                    ["RecipientEmail"] = recipientEmail,
                    ["EntityType"] = entityType ?? (object)DBNull.Value,
                    ["EntityId"] = entityId ?? (object)DBNull.Value,
                    ["TriggeredByUserId"] = triggeredByUserId ?? (object)DBNull.Value
                });

            _logger?.LogInformation(
                "Audit: Notification sent - Event:{EventType}, Type:{NotificationType}, To:{Email}",
                eventType, notificationType, recipientEmail);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging notification sent");
            return false;
        }
    }

    /// <summary>
    /// Log notification delivery failure
    /// </summary>
    public async Task<bool> LogDeliveryFailureAsync(
        int tenantId,
        int notificationId,
        string notificationType,
        string recipientEmail,
        string errorMessage)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationAuditLog
                  (TenantId, NotificationId, RecipientEmail, NotificationType, Action, ErrorMessage, Timestamp)
                  VALUES (@TenantId, @NotificationId, @RecipientEmail, @NotificationType, 'DeliveryFailed',
                          @ErrorMessage, GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["NotificationId"] = notificationId,
                    ["RecipientEmail"] = recipientEmail,
                    ["NotificationType"] = notificationType,
                    ["ErrorMessage"] = errorMessage
                });

            _logger?.LogWarning(
                "Audit: Notification delivery failed - To:{Email}, Error:{Error}",
                recipientEmail, errorMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging delivery failure");
            return false;
        }
    }

    /// <summary>
    /// Log notification read/acknowledged
    /// </summary>
    public async Task<bool> LogNotificationReadAsync(
        int tenantId,
        int notificationId,
        int userId)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationAuditLog
                  (TenantId, NotificationId, RecipientUserId, Action, Timestamp)
                  VALUES (@TenantId, @NotificationId, @RecipientUserId, 'Read', GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["NotificationId"] = notificationId,
                    ["RecipientUserId"] = userId
                });

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging notification read");
            return false;
        }
    }

    /// <summary>
    /// Log retry attempt
    /// </summary>
    public async Task<bool> LogRetryAttemptAsync(
        int tenantId,
        int notificationId,
        int attemptNumber,
        string? reason = null)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationAuditLog
                  (TenantId, NotificationId, Action, ErrorMessage, Timestamp)
                  VALUES (@TenantId, @NotificationId, 'Retry', @Reason, GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["NotificationId"] = notificationId,
                    ["Reason"] = reason ?? $"Retry attempt {attemptNumber}"
                });

            _logger?.LogInformation(
                "Audit: Notification retry - NotificationId:{NotificationId}, Attempt:{AttemptNumber}",
                notificationId, attemptNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging retry attempt");
            return false;
        }
    }

    /// <summary>
    /// Log preference change
    /// </summary>
    public async Task<bool> LogPreferenceChangeAsync(
        int tenantId,
        int userId,
        string setting,
        string oldValue,
        string newValue)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationAuditLog
                  (TenantId, RecipientUserId, Action, ErrorMessage, Timestamp)
                  VALUES (@TenantId, @UserId, 'PreferenceChanged', @Change, GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["UserId"] = userId,
                    ["Change"] = $"{setting}: {oldValue} → {newValue}"
                });

            _logger?.LogInformation(
                "Audit: Preference changed - User:{UserId}, Setting:{Setting}, {OldValue} → {NewValue}",
                userId, setting, oldValue, newValue);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging preference change");
            return false;
        }
    }

    /// <summary>
    /// Get audit trail for notification
    /// </summary>
    public async Task<DataTable> GetAuditTrailAsync(int tenantId, int notificationId)
    {
        try
        {
            var result = await _db.QueryAsync(
                @"SELECT Action, RecipientUserId, RecipientEmail, ErrorMessage, Timestamp
                  FROM dbo.NotificationAuditLog
                  WHERE TenantId = @TenantId AND NotificationId = @NotificationId
                  ORDER BY Timestamp DESC",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["NotificationId"] = notificationId
                });

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving audit trail");
            return new DataTable();
        }
    }

    /// <summary>
    /// Get audit trail for user
    /// </summary>
    public async Task<DataTable> GetUserAuditTrailAsync(int tenantId, int userId, int days = 30)
    {
        try
        {
            var result = await _db.QueryAsync(
                @"SELECT Action, NotificationId, EventType, Timestamp, ErrorMessage
                  FROM dbo.NotificationAuditLog
                  WHERE TenantId = @TenantId AND RecipientUserId = @UserId
                        AND Timestamp >= DATEADD(DAY, -@Days, GETUTCDATE())
                  ORDER BY Timestamp DESC",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["UserId"] = userId,
                    ["Days"] = days
                });

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving user audit trail");
            return new DataTable();
        }
    }

    /// <summary>
    /// Get audit statistics for tenant
    /// </summary>
    public async Task<AuditStatistics> GetAuditStatisticsAsync(int tenantId, int days = 30)
    {
        try
        {
            var result = await _db.QueryAsync(
                @"SELECT
                    COUNT(*) as TotalEvents,
                    SUM(CASE WHEN Action = 'Sent' THEN 1 ELSE 0 END) as NotificationsSent,
                    SUM(CASE WHEN Action = 'DeliveryFailed' THEN 1 ELSE 0 END) as DeliveryFailures,
                    SUM(CASE WHEN Action = 'Read' THEN 1 ELSE 0 END) as NotificationsRead,
                    SUM(CASE WHEN Action = 'Retry' THEN 1 ELSE 0 END) as RetryAttempts,
                    SUM(CASE WHEN Action = 'PreferenceChanged' THEN 1 ELSE 0 END) as PreferenceChanges
                  FROM dbo.NotificationAuditLog
                  WHERE TenantId = @TenantId AND Timestamp >= DATEADD(DAY, -@Days, GETUTCDATE())",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["Days"] = days
                });

            if (result.Rows.Count == 0)
                return new AuditStatistics();

            var row = result.Rows[0];
            return new AuditStatistics
            {
                TotalEvents = (int)row["TotalEvents"],
                NotificationsSent = row["NotificationsSent"] != DBNull.Value ? (int)row["NotificationsSent"] : 0,
                DeliveryFailures = row["DeliveryFailures"] != DBNull.Value ? (int)row["DeliveryFailures"] : 0,
                NotificationsRead = row["NotificationsRead"] != DBNull.Value ? (int)row["NotificationsRead"] : 0,
                RetryAttempts = row["RetryAttempts"] != DBNull.Value ? (int)row["RetryAttempts"] : 0,
                PreferenceChanges = row["PreferenceChanges"] != DBNull.Value ? (int)row["PreferenceChanges"] : 0
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting audit statistics");
            return new AuditStatistics();
        }
    }
}

public class AuditStatistics
{
    public int TotalEvents { get; set; }
    public int NotificationsSent { get; set; }
    public int DeliveryFailures { get; set; }
    public int NotificationsRead { get; set; }
    public int RetryAttempts { get; set; }
    public int PreferenceChanges { get; set; }
}
