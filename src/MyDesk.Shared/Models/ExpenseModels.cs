namespace MyDesk.Shared.Models;

/// <summary>
/// Header/envelope for a submitted expense claim.
/// </summary>
public class ExpenseClaim
{
    public int ClaimId { get; set; }
    public string ClaimRef { get; set; } = "";
    public string ClaimPeriod { get; set; } = "";
    public string SubmittedBy { get; set; } = "";
    public int? SubmittedByUserId { get; set; }
    public int? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal TotalAmount { get; set; }
    public decimal TotalGst { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? FinalisedAt { get; set; }
    public string? FinalisedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<ExpenseClaimItem> Items { get; set; } = new();
}

/// <summary>
/// Individual line item within an expense claim.
/// </summary>
public class ExpenseClaimItem
{
    public int ItemId { get; set; }
    public int ClaimId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Category { get; set; } = "General";
    public string? Supplier { get; set; }
    public string Description { get; set; } = "";
    public decimal AmountExGst { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasGst { get; set; } = true;
    public string? ReceiptFileName { get; set; }
    public string? ReceiptFilePath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
