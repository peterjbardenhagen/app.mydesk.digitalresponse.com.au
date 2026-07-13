namespace MyDesk.Web.Models;

/// <summary>
/// User's display preferences for dashboard
/// </summary>
public class DashboardDisplayPreferences
{
    public bool ShowSummaryCards { get; set; } = true;
    public bool ShowDetailedMetrics { get; set; } = true;
    public bool EnableExport { get; set; } = true;
    public bool ShowComparison { get; set; } = false;
}
