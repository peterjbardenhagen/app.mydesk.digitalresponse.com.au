namespace MyDesk.Shared.Models;

public class AccountingSyncRecord
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = "";       // Xero | QuickBooks | MYOB
    public string EntityType { get; set; } = "";     // Invoice | Contact | Item
    public string ExternalId { get; set; } = "";
    public string InternalId { get; set; } = "";
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public string Direction { get; set; } = "Both";  // Push | Pull | Both
    public string LastStatus { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

public class SyncLogEntry
{
    public int Id { get; set; }
    public string Provider { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Direction { get; set; } = "";
    public int Count { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccountingSyncStatus
{
    public string Provider { get; set; } = "";
    public bool Enabled { get; set; }
    public bool IsConnected { get; set; }
    public DateTime? LastSync { get; set; }
    public string Status { get; set; } = "Not configured";
    public List<SyncLogEntry> RecentLogs { get; set; } = new();
}
