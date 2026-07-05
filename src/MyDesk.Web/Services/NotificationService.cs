using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for sending notifications through multiple channels: Email, SMS, In-App.
/// Handles templating, user preferences, and delivery tracking.
/// </summary>
public class NotificationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<NotificationService>? _logger;

    public NotificationService(DatabaseService db, ILogger<NotificationService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Send notification based on event type and user preferences.
    /// Automatically selects template, checks user preferences, and queues delivery.
    /// </summary>
    public async Task<int> SendNotificationAsync(
        int tenantId,
        int recipientUserId,
        string eventType,
        Dictionary<string, object> placeholders,
        string? entityType = null,
        int? entityId = null,
        int? triggeredByUserId = null)
    {
        try
        {
            _logger?.LogInformation("Sending {EventType} notification to user {UserId} in tenant {TenantId}",
                eventType, recipientUserId, tenantId);

            // Get user preferences
            var prefResult = await _db.QueryAsync(
                @"SELECT EnableEmailNotifications, EmailOnApprovalRequired, EnableInAppNotifications
                  FROM dbo.NotificationSettings
                  WHERE TenantId = @TenantId AND UserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = recipientUserId });

            if (prefResult.Rows.Count == 0)
            {
                _logger?.LogWarning("No notification settings found for user {UserId}", recipientUserId);
                return 0;
            }

            bool enableEmail = (bool)prefResult.Rows[0]["EnableEmailNotifications"];
            bool enableInApp = (bool)prefResult.Rows[0]["EnableInAppNotifications"];

            // Get user email
            var userResult = await _db.QueryAsync(
                "SELECT Email, Name FROM dbo.Users WHERE UserId = @UserId AND TenantId = @TenantId",
                new() { ["UserId"] = recipientUserId, ["TenantId"] = tenantId });

            if (userResult.Rows.Count == 0)
                return 0;

            string recipientEmail = (string)userResult.Rows[0]["Email"];
            string recipientName = (string)userResult.Rows[0]["Name"];

            // Get email template
            var templateResult = await _db.QueryAsync(
                @"SELECT TemplateId, Subject, BodyHtml, InAppTitle, InAppBody, InAppIcon
                  FROM dbo.NotificationTemplates
                  WHERE TenantId = @TenantId AND EventType = @EventType AND NotificationType = 'Email' AND IsActive = 1",
                new() { ["TenantId"] = tenantId, ["EventType"] = eventType });

            if (templateResult.Rows.Count == 0)
            {
                _logger?.LogWarning("No email template found for event {EventType}", eventType);
                return 0;
            }

            var template = templateResult.Rows[0];
            int templateId = (int)template["TemplateId"];
            string subject = (string)template["Subject"];
            string bodyHtml = (string)template["BodyHtml"];
            string inAppTitle = template["InAppTitle"]?.ToString() ?? "";
            string inAppBody = template["InAppBody"]?.ToString() ?? "";
            string inAppIcon = template["InAppIcon"]?.ToString() ?? "";

            // Replace placeholders
            subject = ReplacePlaceholders(subject, placeholders, recipientName);
            bodyHtml = ReplacePlaceholders(bodyHtml, placeholders, recipientName);
            inAppTitle = ReplacePlaceholders(inAppTitle, placeholders, recipientName);
            inAppBody = ReplacePlaceholders(inAppBody, placeholders, recipientName);

            int notificationLogId = 0;

            // Create notification log entry
            if (enableEmail)
            {
                var logResult = await _db.QueryAsync(
                    @"INSERT INTO dbo.NotificationLog
                      (TenantId, EventType, EventEntityType, EventEntityId, TriggeredByUserId, RecipientUserId, RecipientEmail, TemplateId, NotificationType, Subject, BodyPreview, FullContent, Status)
                      OUTPUT INSERTED.NotificationId
                      VALUES (@TenantId, @EventType, @EntityType, @EntityId, @TriggeredBy, @UserId, @Email, @TemplateId, 'Email', @Subject, @Preview, @Body, 'Pending')",
                    new()
                    {
                        ["TenantId"] = tenantId,
                        ["EventType"] = eventType,
                        ["EntityType"] = entityType ?? (object)DBNull.Value,
                        ["EntityId"] = entityId ?? (object)DBNull.Value,
                        ["TriggeredBy"] = triggeredByUserId ?? (object)DBNull.Value,
                        ["UserId"] = recipientUserId,
                        ["Email"] = recipientEmail,
                        ["TemplateId"] = templateId,
                        ["Subject"] = subject,
                        ["Preview"] = bodyHtml.Length > 500 ? bodyHtml.Substring(0, 500) : bodyHtml,
                        ["Body"] = bodyHtml
                    });

                notificationLogId = (int)logResult.Rows[0]["NotificationId"];

                // Queue email for async delivery
                await _db.ExecuteNonQueryAsync(
                    @"INSERT INTO dbo.EmailQueue (TenantId, ToEmail, ToName, FromEmail, FromName, Subject, BodyHtml, NotificationLogId, Status, Priority)
                      VALUES (@TenantId, @Email, @Name, 'noreply@mydesk.app', 'MyDesk', @Subject, @Body, @NotificationLogId, 'Pending', 5)",
                    new()
                    {
                        ["TenantId"] = tenantId,
                        ["Email"] = recipientEmail,
                        ["Name"] = recipientName,
                        ["Subject"] = subject,
                        ["Body"] = bodyHtml,
                        ["NotificationLogId"] = notificationLogId
                    });

                _logger?.LogInformation("Queued email notification {NotificationId}", notificationLogId);
            }

            // Send in-app notification
            if (enableInApp)
            {
                await _db.ExecuteNonQueryAsync(
                    @"INSERT INTO dbo.InAppNotifications (TenantId, UserId, Title, Message, Icon, EntityType, EntityId, Type, Category, CreatedAt)
                      VALUES (@TenantId, @UserId, @Title, @Message, @Icon, @EntityType, @EntityId, 'Action', @Category, GETUTCDATE())",
                    new()
                    {
                        ["TenantId"] = tenantId,
                        ["UserId"] = recipientUserId,
                        ["Title"] = inAppTitle,
                        ["Message"] = inAppBody,
                        ["Icon"] = inAppIcon,
                        ["EntityType"] = entityType ?? (object)DBNull.Value,
                        ["EntityId"] = entityId ?? (object)DBNull.Value,
                        ["Category"] = eventType.Contains("Approval") ? "Approval" : "Notification"
                    });

                // Update notification state
                await IncrementUnreadCountAsync(tenantId, recipientUserId);

                _logger?.LogInformation("Created in-app notification for user {UserId}", recipientUserId);
            }

            return notificationLogId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending notification for event {EventType}", eventType);
            return 0;
        }
    }

    /// <summary>
    /// Send notification to multiple recipients.
    /// </summary>
    public async Task SendBulkNotificationAsync(
        int tenantId,
        List<int> recipientUserIds,
        string eventType,
        Dictionary<string, object> placeholders,
        string? entityType = null,
        int? entityId = null,
        int? triggeredByUserId = null)
    {
        var tasks = recipientUserIds.Select(userId =>
            SendNotificationAsync(tenantId, userId, eventType, placeholders, entityType, entityId, triggeredByUserId));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Get unread notifications for a user.
    /// </summary>
    public async Task<(List<InAppNotification> notifications, int unreadCount)> GetUnreadNotificationsAsync(
        int tenantId,
        int userId,
        int limit = 10)
    {
        var result = await _db.QueryAsync(
            @"SELECT NotificationId, Title, Message, Icon, ActionUrl, ActionText, Type, Category, EntityType, EntityId, CreatedAt, IsRead
              FROM dbo.InAppNotifications
              WHERE TenantId = @TenantId AND UserId = @UserId AND IsRead = 0
              ORDER BY CreatedAt DESC
              OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId, ["Limit"] = limit });

        var notifications = new List<InAppNotification>();
        foreach (var row in result.Rows)
        {
            notifications.Add(new InAppNotification
            {
                NotificationId = (int)row["NotificationId"],
                Title = (string)row["Title"],
                Message = (string)row["Message"],
                Icon = row["Icon"]?.ToString(),
                ActionUrl = row["ActionUrl"]?.ToString(),
                ActionText = row["ActionText"]?.ToString(),
                Type = (string)row["Type"],
                Category = (string)row["Category"],
                EntityType = row["EntityType"]?.ToString(),
                EntityId = row["EntityId"] != DBNull.Value ? (int?)row["EntityId"] : null,
                CreatedAt = (DateTime)row["CreatedAt"],
                IsRead = (bool)row["IsRead"]
            });
        }

        // Get unread count
        var countResult = await _db.QueryAsync(
            "SELECT UnreadTotal FROM dbo.NotificationState WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        int unreadCount = countResult.Rows.Count > 0 ? (int)countResult.Rows[0]["UnreadTotal"] : 0;

        return (notifications, unreadCount);
    }

    /// <summary>
    /// Mark notification as read.
    /// </summary>
    public async Task MarkAsReadAsync(int notificationId)
    {
        await _db.ExecuteNonQueryAsync(
            @"UPDATE dbo.InAppNotifications SET IsRead = 1, ReadAt = GETUTCDATE() WHERE NotificationId = @NotificationId",
            new() { ["NotificationId"] = notificationId });
    }

    /// <summary>
    /// Mark all notifications as read for a user.
    /// </summary>
    public async Task MarkAllAsReadAsync(int tenantId, int userId)
    {
        await _db.ExecuteNonQueryAsync(
            @"UPDATE dbo.InAppNotifications SET IsRead = 1, ReadAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND UserId = @UserId AND IsRead = 0",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        // Reset unread count
        await _db.ExecuteNonQueryAsync(
            @"UPDATE dbo.NotificationState SET UnreadTotal = 0 WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });
    }

    /// <summary>
    /// Update user notification preferences.
    /// </summary>
    public async Task UpdatePreferencesAsync(
        int tenantId,
        int userId,
        NotificationPreferences preferences)
    {
        await _db.ExecuteNonQueryAsync(
            @"UPDATE dbo.NotificationSettings
              SET EnableEmailNotifications = @EmailEnabled,
                  EmailOnApprovalRequired = @EmailApproval,
                  EmailDigestFrequency = @DigestFreq,
                  EnableSmsNotifications = @SmsEnabled,
                  PhoneNumber = @Phone,
                  EnableInAppNotifications = @InAppEnabled,
                  QuietHoursEnabled = @QuietEnabled,
                  QuietHoursStart = @QuietStart,
                  QuietHoursEnd = @QuietEnd,
                  ModifiedAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND UserId = @UserId",
            new()
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["EmailEnabled"] = preferences.EnableEmailNotifications,
                ["EmailApproval"] = preferences.EmailOnApprovalRequired,
                ["DigestFreq"] = preferences.EmailDigestFrequency,
                ["SmsEnabled"] = preferences.EnableSmsNotifications,
                ["Phone"] = preferences.PhoneNumber ?? (object)DBNull.Value,
                ["InAppEnabled"] = preferences.EnableInAppNotifications,
                ["QuietEnabled"] = preferences.QuietHoursEnabled,
                ["QuietStart"] = preferences.QuietHoursStart ?? (object)DBNull.Value,
                ["QuietEnd"] = preferences.QuietHoursEnd ?? (object)DBNull.Value
            });
    }

    private string ReplacePlaceholders(string text, Dictionary<string, object> placeholders, string recipientName)
    {
        var result = text;
        foreach (var kvp in placeholders)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            var value = kvp.Value?.ToString() ?? "";
            result = result.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }
        result = result.Replace("{{RecipientName}}", recipientName, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private async Task IncrementUnreadCountAsync(int tenantId, int userId)
    {
        var existsResult = await _db.QueryAsync(
            "SELECT StateId FROM dbo.NotificationState WHERE TenantId = @TenantId AND UserId = @UserId",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });

        if (existsResult.Rows.Count == 0)
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO dbo.NotificationState (TenantId, UserId, UnreadTotal, UpdatedAt)
                  VALUES (@TenantId, @UserId, 1, GETUTCDATE())",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });
        }
        else
        {
            await _db.ExecuteNonQueryAsync(
                @"UPDATE dbo.NotificationState
                  SET UnreadTotal = UnreadTotal + 1, UpdatedAt = GETUTCDATE()
                  WHERE TenantId = @TenantId AND UserId = @UserId",
                new() { ["TenantId"] = tenantId, ["UserId"] = userId });
        }
    }
}

public class InAppNotification
{
    public int NotificationId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Icon { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class NotificationPreferences
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EmailOnApprovalRequired { get; set; } = true;
    public string EmailDigestFrequency { get; set; } = "Immediate";
    public bool EnableSmsNotifications { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public bool EnableInAppNotifications { get; set; } = true;
    public bool QuietHoursEnabled { get; set; } = false;
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}
