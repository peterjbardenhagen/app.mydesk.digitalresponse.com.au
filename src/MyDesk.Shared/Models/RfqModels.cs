namespace MyDesk.Shared.Models;

// ============================================================================
// RFQ (Request For Quote) domain models
// Ported from legacy MyDesk - in-memory only for now.
// TODO: persist to database
// ============================================================================
public enum RfqStatus
{
    Draft = 0,
    Sent = 1,
    Responded = 2,
    Awarded = 3,
    Cancelled = 4
}

public class Rfq
{
    public int Id { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? RequiredByDate { get; set; }
    public RfqStatus Status { get; set; } = RfqStatus.Draft;
    public List<int> Suppliers { get; set; } = new();
    public List<RfqResponse> Responses { get; set; } = new();
    public int? WinningResponseId { get; set; }
    public int? Qid { get; set; }
    public string? OwnerUserCode { get; set; }
    public string? OwnerName { get; set; }
}

public class RfqResponse
{
    public int Id { get; set; }
    public int RfqId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal QuotedPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Notes { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.Now;
}
