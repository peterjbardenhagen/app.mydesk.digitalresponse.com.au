namespace MyDesk.Shared.Models;

// ============================================================================
// Sales Projects (sales pipeline) — ported from legacy MyDesk
// In-memory only.  TODO: persist to database
// ============================================================================
public enum SalesStage
{
    Lead = 0,
    Qualified = 1,
    Proposal = 2,
    Negotiation = 3,
    Won = 4,
    Lost = 5
}

public class SalesProject
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public int ProbabilityPercent { get; set; }
    public SalesStage Stage { get; set; } = SalesStage.Lead;
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ActualCloseDate { get; set; }
    public string? OwnerUserCode { get; set; }
    public string? OwnerName { get; set; }
    public string? Notes { get; set; }
    public List<int> LinkedQuoteIds { get; set; } = new();
    public List<int> LinkedInvoiceIds { get; set; } = new();
    public List<int> LinkedPoIds { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public decimal WeightedValue => EstimatedValue * ProbabilityPercent / 100m;
    public bool IsClosed => Stage == SalesStage.Won || Stage == SalesStage.Lost;
}

public class WinLossStats
{
    public int WonCount { get; set; }
    public int LostCount { get; set; }
    public decimal WonValue { get; set; }
    public decimal LostValue { get; set; }
    public decimal WinRate => (WonCount + LostCount) == 0
        ? 0
        : (decimal)WonCount * 100m / (WonCount + LostCount);
}
