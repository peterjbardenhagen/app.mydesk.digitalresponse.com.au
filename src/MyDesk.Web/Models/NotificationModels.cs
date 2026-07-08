namespace MyDesk.Web.Models;

public class NotificationLog
{
    public int notificationId { get; set; }
    public string eventType { get; set; } = "";
    public string notificationType { get; set; } = "";
    public string status { get; set; } = "";
    public DateTime createdAt { get; set; }
    public DateTime? sentAt { get; set; }
    public DateTime? failedAt { get; set; }
    public string? errorMessage { get; set; }
    public string recipientEmail { get; set; } = "";
    public string subject { get; set; } = "";
}

public class QueueStatus
{
    public int pending { get; set; }
    public int sent { get; set; }
    public int failed { get; set; }
}

public class RetryResult
{
    public int retriedCount { get; set; }
}
