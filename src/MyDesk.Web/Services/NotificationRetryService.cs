using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Notification Retry Service: Handle failed notification delivery with exponential backoff
/// Part of Phase 5: Notifications & Alerts
/// </summary>
public class NotificationRetryService
{
    private readonly DatabaseService _db;
    private readonly ILogger<NotificationRetryService>? _logger;

    // Retry configuration
    private const int MaxRetries = 5;
    private const int InitialDelaySeconds = 60;      // 1 minute
    private const int MaxDelaySeconds = 86400;       // 24 hours
    private const double BackoffMultiplier = 2.0;    // Exponential backoff

    public NotificationRetryService(
        DatabaseService db,
        ILogger<NotificationRetryService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Process failed notifications and schedule retries
    /// Should be called by background job periodically
    /// </summary>
    public async Task<int> ProcessFailedNotificationsAsync(int tenantId)
    {
        try
        {
            _logger?.LogInformation("Processing failed notifications for tenant {TenantId}", tenantId);

            // Get failed notifications eligible for retry
            var failedResult = await _db.QueryAsync(
                @"SELECT EmailQueueId, ToEmail, ToName, FromEmail, FromName, Subject, BodyHtml,
                         NotificationLogId, RetryCount, LastRetryAt, ErrorMessage
                  FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId
                        AND Status = 'Failed'
                        AND RetryCount < @MaxRetries
                        AND (LastRetryAt IS NULL OR
                             DATEADD(SECOND, @NextRetryDelay, LastRetryAt) <= GETUTCDATE())
                  ORDER BY LastRetryAt ASC",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["MaxRetries"] = MaxRetries,
                    ["NextRetryDelay"] = InitialDelaySeconds
                });

            int retryCount = 0;

            foreach (DataRow row in failedResult.Rows)
            {
                var emailQueueId = (int)row["EmailQueueId"];
                var retryAttempt = row["RetryCount"] != DBNull.Value ? (int)row["RetryCount"] : 0;

                // Calculate next retry delay using exponential backoff
                int nextDelay = CalculateNextDelay(retryAttempt);

                _logger?.LogInformation(
                    "Retrying failed email {EmailQueueId} (attempt {Attempt}), next retry in {Delay}s",
                    emailQueueId, retryAttempt + 1, nextDelay);

                // Update retry status
                await _db.ExecuteNonQueryAsync(
                    @"UPDATE dbo.EmailQueue
                      SET Status = 'Pending',
                          RetryCount = RetryCount + 1,
                          LastRetryAt = GETUTCDATE(),
                          ScheduledRetryAt = DATEADD(SECOND, @Delay, GETUTCDATE()),
                          UpdatedAt = GETUTCDATE()
                      WHERE EmailQueueId = @Id",
                    new() { ["Id"] = emailQueueId, ["Delay"] = nextDelay });

                // Log retry attempt
                await _db.ExecuteNonQueryAsync(
                    @"INSERT INTO dbo.NotificationRetryLog
                      (TenantId, EmailQueueId, NotificationLogId, AttemptNumber, NextRetryAt, CreatedAt)
                      VALUES (@TenantId, @EmailQueueId, @NotificationLogId, @Attempt,
                              DATEADD(SECOND, @Delay, GETUTCDATE()), GETUTCDATE())",
                    new()
                    {
                        ["TenantId"] = tenantId,
                        ["EmailQueueId"] = emailQueueId,
                        ["NotificationLogId"] = row["NotificationLogId"],
                        ["Attempt"] = retryAttempt + 1,
                        ["Delay"] = nextDelay
                    });

                retryCount++;
            }

            _logger?.LogInformation("Queued {Count} notifications for retry", retryCount);
            return retryCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing failed notifications");
            return 0;
        }
    }

    /// <summary>
    /// Mark email as permanently failed (max retries exceeded)
    /// </summary>
    public async Task<bool> MarkAsPermanentlyFailedAsync(int emailQueueId, string finalErrorMessage)
    {
        try
        {
            _logger?.LogWarning("Marking email {EmailQueueId} as permanently failed: {Error}",
                emailQueueId, finalErrorMessage);

            var result = await _db.QueryAsync(
                "SELECT NotificationLogId FROM dbo.EmailQueue WHERE EmailQueueId = @Id",
                new() { ["Id"] = emailQueueId });

            if (result.Rows.Count == 0)
                return false;

            int notificationLogId = (int)result.Rows[0]["NotificationLogId"];

            // Update email queue
            await _db.ExecuteNonQueryAsync(
                @"UPDATE dbo.EmailQueue
                  SET Status = 'DeadLettered',
                      ErrorMessage = @Error,
                      UpdatedAt = GETUTCDATE()
                  WHERE EmailQueueId = @Id",
                new() { ["Id"] = emailQueueId, ["Error"] = finalErrorMessage });

            // Update notification log
            await _db.ExecuteNonQueryAsync(
                @"UPDATE dbo.NotificationLog
                  SET Status = 'Failed',
                      FailedAt = GETUTCDATE(),
                      ErrorMessage = @Error
                  WHERE NotificationId = @Id",
                new() { ["Id"] = notificationLogId, ["Error"] = finalErrorMessage });

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error marking email as permanently failed");
            return false;
        }
    }

    /// <summary>
    /// Manually retry specific failed email
    /// </summary>
    public async Task<bool> RetrySpecificEmailAsync(int emailQueueId)
    {
        try
        {
            _logger?.LogInformation("Manual retry requested for email {EmailQueueId}", emailQueueId);

            var result = await _db.QueryAsync(
                @"SELECT RetryCount FROM dbo.EmailQueue WHERE EmailQueueId = @Id",
                new() { ["Id"] = emailQueueId });

            if (result.Rows.Count == 0)
                return false;

            int currentRetries = (int)result.Rows[0]["RetryCount"];

            if (currentRetries >= MaxRetries)
            {
                _logger?.LogWarning("Email {EmailQueueId} has exceeded max retries ({Count})",
                    emailQueueId, MaxRetries);
                return false;
            }

            await _db.ExecuteNonQueryAsync(
                @"UPDATE dbo.EmailQueue
                  SET Status = 'Pending',
                      RetryCount = RetryCount + 1,
                      LastRetryAt = GETUTCDATE(),
                      ErrorMessage = NULL,
                      UpdatedAt = GETUTCDATE()
                  WHERE EmailQueueId = @Id",
                new() { ["Id"] = emailQueueId });

            _logger?.LogInformation("Email {EmailQueueId} queued for manual retry", emailQueueId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrying email");
            return false;
        }
    }

    /// <summary>
    /// Get retry history for an email
    /// </summary>
    public async Task<DataTable> GetRetryHistoryAsync(int emailQueueId)
    {
        try
        {
            var result = await _db.QueryAsync(
                @"SELECT AttemptNumber, NextRetryAt, CreatedAt
                  FROM dbo.NotificationRetryLog
                  WHERE EmailQueueId = @Id
                  ORDER BY AttemptNumber DESC",
                new() { ["Id"] = emailQueueId });

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting retry history");
            return new DataTable();
        }
    }

    /// <summary>
    /// Get statistics on failed notifications
    /// </summary>
    public async Task<FailureStatistics> GetFailureStatisticsAsync(int tenantId)
    {
        try
        {
            var result = await _db.QueryAsync(
                @"SELECT
                    COUNT(*) as TotalFailed,
                    SUM(CASE WHEN RetryCount = 0 THEN 1 ELSE 0 END) as NeverRetried,
                    SUM(CASE WHEN RetryCount > 0 AND RetryCount < @MaxRetries THEN 1 ELSE 0 END) as InRetry,
                    SUM(CASE WHEN RetryCount >= @MaxRetries THEN 1 ELSE 0 END) as ExceededMax
                  FROM dbo.EmailQueue
                  WHERE TenantId = @TenantId AND Status IN ('Failed', 'DeadLettered')",
                new() { ["TenantId"] = tenantId, ["MaxRetries"] = MaxRetries });

            if (result.Rows.Count == 0)
                return new FailureStatistics { TotalFailed = 0 };

            var row = result.Rows[0];
            return new FailureStatistics
            {
                TotalFailed = (int)row["TotalFailed"],
                NeverRetried = row["NeverRetried"] != DBNull.Value ? (int)row["NeverRetried"] : 0,
                InRetry = row["InRetry"] != DBNull.Value ? (int)row["InRetry"] : 0,
                ExceededMax = row["ExceededMax"] != DBNull.Value ? (int)row["ExceededMax"] : 0
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting failure statistics");
            return new FailureStatistics();
        }
    }

    private int CalculateNextDelay(int retryAttempt)
    {
        // Exponential backoff: 1min, 2min, 4min, 8min, 16min, 32min, ...
        int delay = (int)(InitialDelaySeconds * Math.Pow(BackoffMultiplier, retryAttempt));

        // Cap at max delay
        return Math.Min(delay, MaxDelaySeconds);
    }
}

public class FailureStatistics
{
    public int TotalFailed { get; set; }
    public int NeverRetried { get; set; }
    public int InRetry { get; set; }
    public int ExceededMax { get; set; }
}
