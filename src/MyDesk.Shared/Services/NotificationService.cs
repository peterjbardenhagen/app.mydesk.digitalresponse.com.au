using MyDesk.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class NotificationService
{
    private readonly DashboardService _dashboard;
    private readonly ActivityService _activity;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DashboardService dashboard, ActivityService activity, ILogger<NotificationService> logger)
    {
        _dashboard = dashboard;
        _activity = activity;
        _logger = logger;
    }

    public async Task<List<NotificationItem>> GetNotificationsAsync(string? userCode = null)
    {
        var notifications = new List<NotificationItem>();
        try
        {
            // 1. Get Warnings and Recommendations from Dashboard (Business Health)
            var metrics = await _dashboard.GetMetricsAsync();
            
            foreach (var w in metrics.Warnings)
            {
                notifications.Add(new NotificationItem
                {
                    Title = w.Title,
                    Message = w.Description,
                    Type = w.Severity == "critical" ? "error" : "warning",
                    Time = "Action Required",
                    Url = w.ActionLink
                });
            }

            foreach (var r in metrics.Recommendations)
            {
                notifications.Add(new NotificationItem
                {
                    Title = r.Title,
                    Message = r.Description,
                    Type = "info",
                    Time = "Recommendation",
                    Url = r.ActionLink
                });
            }

            // 2. Get Recent Activities (System events)
            var activities = await _activity.GetRecentAsync(10);
            foreach (var a in activities)
            {
                notifications.Add(new NotificationItem
                {
                    Title = $"{a.EntityType} {a.EntityRef}",
                    Message = $"{a.UserName}: {a.Action}",
                    Type = "info",
                    Time = FormatRelativeTime(a.ActivityDate),
                    Url = GetUrlForEntity(a.EntityType, a.EntityId)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching real notifications");
        }

        // De-duplicate and order
        return notifications
            .GroupBy(n => n.Title + n.Message)
            .Select(g => g.First())
            .OrderByDescending(n => n.Time == "Action Required" ? 1 : 0)
            .ToList();
    }

    private string GetUrlForEntity(string type, int? id) => type.ToLower() switch
    {
        "quote" => $"/quotes/{id}",
        "invoice" => $"/invoices/{id}",
        "po" or "purchaseorder" => $"/purchase-orders/{id}",
        "despatch" => $"/despatch",
        _ => "#"
    };

    private string FormatRelativeTime(DateTime dt)
    {
        if (dt == DateTime.MinValue) return "Recently";
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return dt.ToString("MMM d");
    }
}

public class NotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info"; // info, success, warning, error
    public string Time { get; set; } = "";
    public string? Url { get; set; }
}
