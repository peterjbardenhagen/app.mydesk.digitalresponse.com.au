namespace MyDesk.Shared.Models;

// ============================================================================
// Vehicle maintenance PO request — ported from legacy MyDesk
// In-memory only.  TODO: persist to database
// ============================================================================
public class PoRequest
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.Now;
    public string? RequesterUserCode { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterEmail { get; set; }
    public string Division { get; set; } = "Traffic Mgmt"; // Traffic Mgmt, QLD, NSW, VIC, SA
    public string VehicleRegistration { get; set; } = string.Empty;
    public string? VehicleDescription { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }
    public string MaintenanceType { get; set; } = string.Empty; // Service, Tyres, Repair, Other
    public string? Description { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted, Approved, Rejected
    public string? RoutedToEmail { get; set; }
}
