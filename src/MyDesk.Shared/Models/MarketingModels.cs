namespace MyDesk.Shared.Models;

// ============================================================================
// Customer Data Platform — detailed RFM + health scoring
// ============================================================================
public class CustomerScoreCard
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public decimal LifetimeRevenue { get; set; }
    public decimal YtdRevenue { get; set; }
    public decimal LastYearRevenue { get; set; }
    public int InvoiceCount { get; set; }
    public int QuoteCount { get; set; }
    public int DeliveryCount { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public DateTime? FirstActivity { get; set; }
    public DateTime? LastActivity { get; set; }
    public int DaysSinceLastActivity { get; set; }
    public int CustomerLifetimeDays { get; set; }
    public decimal GrowthPercent { get; set; }
    public decimal WinRate { get; set; }          // quotes won / quotes raised
    public decimal EffortToValueRatio { get; set; } // quote value : revenue
    public decimal OutstandingReceivables { get; set; }
    public int OverdueInvoices { get; set; }

    // ── RFM Scoring (1-5 each) ─────────────────────────────────────────────
    public int RecencyScore { get; set; }    // How recently (lower DaysSinceLastActivity = higher)
    public int FrequencyScore { get; set; }  // How often (higher InvoiceCount = higher)
    public int MonetaryScore { get; set; }   // How much (higher YtdRevenue = higher)

    // ── Derived ────────────────────────────────────────────────────────────
    public int TotalScore => RecencyScore + FrequencyScore + MonetaryScore; // /15
    public string Segment { get; set; } = "";     // Champion, Loyal, Promising, At-Risk, Lost, etc.
    public string Rating  { get; set; } = "";     // Diamond / Gold / Silver / Bronze / Watch
    public string HealthStatus { get; set; } = ""; // healthy, neutral, at-risk, critical
    public int Rank { get; set; }

    public List<string> SignalsPositive { get; set; } = new();
    public List<string> SignalsNegative { get; set; } = new();
}

// ============================================================================
// Supplier Data Platform — scoring for inbound side
// ============================================================================
public class SupplierScoreCard
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public decimal LifetimeSpend { get; set; }
    public decimal YtdSpend { get; set; }
    public decimal LastYearSpend { get; set; }
    public int POCount { get; set; }
    public int OpenPOCount { get; set; }
    public decimal OpenPOValue { get; set; }
    public decimal AveragePOValue { get; set; }
    public DateTime? FirstPO { get; set; }
    public DateTime? LastPO { get; set; }
    public int DaysSinceLastPO { get; set; }
    public decimal SpendGrowthPercent { get; set; }

    public int DependencyScore { get; set; }   // 1-5: how dependent we are on them
    public int ReliabilityScore { get; set; }  // 1-5: inferred from PO completion patterns
    public int ValueScore { get; set; }        // 1-5: relative spend band

    public int TotalScore => DependencyScore + ReliabilityScore + ValueScore;  // /15
    public string Tier { get; set; } = "";    // Strategic, Preferred, Transactional, Trial
    public string Region { get; set; } = "";
    public int Rank { get; set; }

    public List<string> Signals { get; set; } = new();
}

public class CustomerDataPlatform
{
    public List<CustomerScoreCard> All { get; set; } = new();
    public Dictionary<string, int> SegmentCounts { get; set; } = new();
    public decimal TotalLifetimeRevenue { get; set; }
    public decimal TotalYtdRevenue { get; set; }
    public int Top10PercentRevenueShare { get; set; }
    public CustomerScoreCard? Champion { get; set; }
}

public class SupplierDataPlatform
{
    public List<SupplierScoreCard> All { get; set; } = new();
    public Dictionary<string, int> TierCounts { get; set; } = new();
    public decimal TotalLifetimeSpend { get; set; }
    public decimal TotalYtdSpend { get; set; }
    public int StrategicSupplierCount { get; set; }
}

// ============================================================================
// Marketing Strategy (persisted JSON document)
// ============================================================================
public class MarketingStrategy
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Draft"; // Draft, Active, Archived

    public string Vision { get; set; } = "";
    public string IdealCustomerProfile { get; set; } = "";
    public string IdealSupplierProfile { get; set; } = "";
    public string TargetMarkets { get; set; } = "";
    public string Positioning { get; set; } = "";
    public string ValueProposition { get; set; } = "";
    public string CompetitorAnalysis { get; set; } = "";
    public string Channels { get; set; } = "";
    public string KeyInitiatives { get; set; } = "";
    public string KpiTargets { get; set; } = "";
    public string Budget { get; set; } = "";
    public string Notes { get; set; } = "";
}

// ============================================================================
// Email Campaigns
// ============================================================================
public class EmailCampaign
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string FromName { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ReplyTo { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public string Audience { get; set; } = ""; // champions, top-50-customers, top-50-suppliers, custom
    public List<string> CustomEmails { get; set; } = new();
    public string Status { get; set; } = "Draft"; // Draft, Sending, Sent, Failed
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int RecipientCount { get; set; }
    public int OpenRate { get; set; }
    public int ClickRate { get; set; }
    public List<string> Log { get; set; } = new();
}

public class CampaignRecipient
{
    public int? CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Reason { get; set; } = "";    // Why they're in this audience
}

// ============================================================================
// Campaign Statistics
// ============================================================================
public class CampaignStats
{
    public int TotalSent { get; set; }
    public int AvgOpenRate { get; set; }
    public int AvgClickRate { get; set; }
}

// ============================================================================
// Marketing Strategy Document (simplified for v1)
// ============================================================================
public class MarketingStrategyDoc
{
    public string IcpIndustries { get; set; } = "";
    public string IcpCompanySize { get; set; } = "";
    public string IcpPainPoints { get; set; } = "";
    public string IcpBuyingTriggers { get; set; } = "";
    public string ValueProposition { get; set; } = "";
    public string Differentiators { get; set; } = "";
    public string PositioningStatement { get; set; } = "";
    public string Q1Initiatives { get; set; } = "";
    public string Q2Initiatives { get; set; } = "";
    public string Q3Initiatives { get; set; } = "";
    public string Q4Initiatives { get; set; } = "";
    public int KpiLeadTarget { get; set; }
    public decimal KpiConversionRate { get; set; }
    public decimal KpiCacTarget { get; set; }
    public int KpiNpsTarget { get; set; }
    public string Notes { get; set; } = "";
}
