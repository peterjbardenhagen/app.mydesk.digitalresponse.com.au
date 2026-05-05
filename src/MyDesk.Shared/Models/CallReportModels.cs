namespace MyDesk.Shared.Models;

// ============================================================================
// Call Reports — ported from legacy MyDesk
// In-memory only.  TODO: persist to database
// ============================================================================
public enum CallType
{
    Phone = 0,
    Visit = 1,
    Email = 2,
    Meeting = 3,
    Video = 4
}

public class CallReport
{
    public int Id { get; set; }
    public DateTime CallDate { get; set; } = DateTime.Now;
    public int? ContactId { get; set; }
    public string? ContactName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? OwnerUserCode { get; set; }
    public string? OwnerName { get; set; }
    public CallType CallType { get; set; } = CallType.Phone;
    public string Subject { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public bool FollowUpComplete { get; set; }
    public int? SalesProjectId { get; set; }
}
