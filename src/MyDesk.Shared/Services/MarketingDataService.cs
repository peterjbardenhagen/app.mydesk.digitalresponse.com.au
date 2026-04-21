using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Customer & Supplier Data Platform.
/// Deep RFM-style scoring of Companies based on Quotes / Invoices / Purchase Orders.
/// </summary>
public class MarketingDataService
{
    private readonly DatabaseService _db;
    private readonly ILogger<MarketingDataService> _logger;

    public MarketingDataService(DatabaseService db, ILogger<MarketingDataService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CUSTOMER DATA PLATFORM
    // ════════════════════════════════════════════════════════════════════════
    public async Task<CustomerDataPlatform> GetCustomerDataAsync()
    {
        var cdp = new CustomerDataPlatform();
        try
        {
            var now = DateTime.Now;
            var yearStart = new DateTime(now.Year, 1, 1);
            var lastYearStart = new DateTime(now.Year - 1, 1, 1);
            var lastYearCutoff = now.AddYears(-1);

            // One query for all customer metrics
            var sql = @"
                WITH Inv AS (
                    SELECT ct.CompanyId,
                           ISNULL(SUM(CASE WHEN i.InvoiceDate >= @Y THEN i.NettPriceTotal ELSE 0 END), 0) AS YtdRevenue,
                           ISNULL(SUM(CASE WHEN i.InvoiceDate >= @LY AND i.InvoiceDate <= @LYE THEN i.NettPriceTotal ELSE 0 END), 0) AS LyRevenue,
                           ISNULL(SUM(i.NettPriceTotal), 0) AS Lifetime,
                           COUNT(i.InvoiceId) AS InvCount,
                           MIN(i.InvoiceDate) AS FirstInv,
                           MAX(i.InvoiceDate) AS LastInv
                    FROM Contacts ct
                    LEFT JOIN Invoices i ON i.ContactId = ct.ContactId
                    GROUP BY ct.CompanyId
                ),
                Qot AS (
                    SELECT ct.CompanyId,
                           COUNT(q.Qid) AS QCount,
                           ISNULL(SUM(q.NettPriceTotal), 0) AS QValue,
                           SUM(CASE WHEN q.QuoteStatusId IN (4,10) THEN 1 ELSE 0 END) AS QWon
                    FROM Contacts ct
                    LEFT JOIN Quotes q ON q.ContactId = ct.ContactId
                    GROUP BY ct.CompanyId
                )
                SELECT c.CompanyId, c.Company,
                       ISNULL(i.YtdRevenue, 0) AS YtdRevenue,
                       ISNULL(i.LyRevenue, 0)  AS LyRevenue,
                       ISNULL(i.Lifetime, 0)   AS Lifetime,
                       ISNULL(i.InvCount, 0)   AS InvCount,
                       i.FirstInv, i.LastInv,
                       ISNULL(q.QCount, 0)     AS QCount,
                       ISNULL(q.QValue, 0)     AS QValue,
                       ISNULL(q.QWon, 0)       AS QWon
                FROM Companies c
                LEFT JOIN Inv i ON i.CompanyId = c.CompanyId
                LEFT JOIN Qot q ON q.CompanyId = c.CompanyId
                WHERE c.Company IS NOT NULL AND c.Company <> ''
                  AND (ISNULL(i.Lifetime, 0) > 0 OR ISNULL(q.QValue, 0) > 0)";

            var dt = await _db.QueryAsync(sql, new()
            {
                ["Y"] = yearStart, ["LY"] = lastYearStart, ["LYE"] = lastYearCutoff
            });

            var cards = new List<CustomerScoreCard>();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                var c = new CustomerScoreCard
                {
                    CompanyId = Convert.ToInt32(r["CompanyId"]),
                    CompanyName = r["Company"].ToString() ?? "",
                    YtdRevenue = Convert.ToDecimal(r["YtdRevenue"]),
                    LastYearRevenue = Convert.ToDecimal(r["LyRevenue"]),
                    LifetimeRevenue = Convert.ToDecimal(r["Lifetime"]),
                    InvoiceCount = Convert.ToInt32(r["InvCount"]),
                    QuoteCount = Convert.ToInt32(r["QCount"])
                };

                if (r["FirstInv"] != DBNull.Value)
                {
                    c.FirstActivity = Convert.ToDateTime(r["FirstInv"]);
                    c.CustomerLifetimeDays = (now - c.FirstActivity.Value).Days;
                }
                if (r["LastInv"] != DBNull.Value)
                {
                    c.LastActivity = Convert.ToDateTime(r["LastInv"]);
                    c.DaysSinceLastActivity = (now - c.LastActivity.Value).Days;
                }
                else
                {
                    c.DaysSinceLastActivity = 999;
                }

                if (c.InvoiceCount > 0)
                    c.AverageInvoiceValue = c.LifetimeRevenue / c.InvoiceCount;

                var qWon = Convert.ToInt32(r["QWon"]);
                if (c.QuoteCount > 0) c.WinRate = ((decimal)qWon / c.QuoteCount) * 100;

                var qValue = Convert.ToDecimal(r["QValue"]);
                if (c.YtdRevenue > 0 && qValue > 0) c.EffortToValueRatio = qValue / c.YtdRevenue;

                if (c.LastYearRevenue > 0)
                    c.GrowthPercent = ((c.YtdRevenue - c.LastYearRevenue) / c.LastYearRevenue) * 100;
                else if (c.YtdRevenue > 0) c.GrowthPercent = 100;

                cards.Add(c);
            }

            // ── Score RFM (percentile-based 1-5) ────────────────────────────
            ScoreRecency(cards);
            ScoreFrequency(cards);
            ScoreMonetary(cards);

            // ── Segment + Rate + Signals ────────────────────────────────────
            foreach (var c in cards)
            {
                c.Segment = ClassifySegment(c);
                c.Rating  = ClassifyRating(c);
                c.HealthStatus = ClassifyHealth(c);
                BuildSignals(c);
            }

            // Rank by total score then YTD revenue
            var ranked = cards
                .OrderByDescending(c => c.TotalScore)
                .ThenByDescending(c => c.YtdRevenue)
                .ToList();
            for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;

            cdp.All = ranked;
            cdp.SegmentCounts = ranked.GroupBy(c => c.Segment).ToDictionary(g => g.Key, g => g.Count());
            cdp.TotalLifetimeRevenue = ranked.Sum(c => c.LifetimeRevenue);
            cdp.TotalYtdRevenue = ranked.Sum(c => c.YtdRevenue);
            cdp.Champion = ranked.FirstOrDefault();

            if (cdp.TotalYtdRevenue > 0)
            {
                var top10 = ranked.Take((int)Math.Max(1, ranked.Count * 0.1)).Sum(c => c.YtdRevenue);
                cdp.Top10PercentRevenueShare = (int)((top10 / cdp.TotalYtdRevenue) * 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building customer data platform");
        }
        return cdp;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SUPPLIER DATA PLATFORM
    // ════════════════════════════════════════════════════════════════════════
    public async Task<SupplierDataPlatform> GetSupplierDataAsync()
    {
        var sdp = new SupplierDataPlatform();
        try
        {
            var now = DateTime.Now;
            var yearStart = new DateTime(now.Year, 1, 1);
            var lastYearStart = new DateTime(now.Year - 1, 1, 1);
            var lastYearCutoff = now.AddYears(-1);

            var sql = @"
                SELECT c.CompanyId, c.Company, c.Country,
                       ISNULL(SUM(CASE WHEN p.PODate >= @Y THEN p.PriceExTotal ELSE 0 END), 0) AS YtdSpend,
                       ISNULL(SUM(CASE WHEN p.PODate >= @LY AND p.PODate <= @LYE THEN p.PriceExTotal ELSE 0 END), 0) AS LySpend,
                       ISNULL(SUM(p.PriceExTotal), 0) AS Lifetime,
                       COUNT(p.POid)                     AS POCount,
                       SUM(CASE WHEN p.POStatusId IN (1,2,3) THEN 1 ELSE 0 END) AS OpenCount,
                       SUM(CASE WHEN p.POStatusId IN (1,2,3) THEN p.PriceExTotal ELSE 0 END) AS OpenValue,
                       MIN(p.PODate) AS FirstPO,
                       MAX(p.PODate) AS LastPO
                FROM Companies c
                LEFT JOIN Contacts ct ON ct.CompanyId = c.CompanyId
                LEFT JOIN PurchaseOrders p ON p.ContactId = ct.ContactId
                WHERE c.Company IS NOT NULL AND c.Company <> ''
                GROUP BY c.CompanyId, c.Company, c.Country
                HAVING ISNULL(SUM(p.PriceExTotal), 0) > 0";

            var dt = await _db.QueryAsync(sql, new()
            {
                ["Y"] = yearStart, ["LY"] = lastYearStart, ["LYE"] = lastYearCutoff
            });

            var cards = new List<SupplierScoreCard>();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                var s = new SupplierScoreCard
                {
                    CompanyId = Convert.ToInt32(r["CompanyId"]),
                    CompanyName = r["Company"].ToString() ?? "",
                    Region = (r["Country"] ?? "").ToString() ?? "",
                    YtdSpend = Convert.ToDecimal(r["YtdSpend"]),
                    LastYearSpend = Convert.ToDecimal(r["LySpend"]),
                    LifetimeSpend = Convert.ToDecimal(r["Lifetime"]),
                    POCount = Convert.ToInt32(r["POCount"]),
                    OpenPOCount = Convert.ToInt32(r["OpenCount"]),
                    OpenPOValue = Convert.ToDecimal(r["OpenValue"])
                };

                if (r["FirstPO"] != DBNull.Value) s.FirstPO = Convert.ToDateTime(r["FirstPO"]);
                if (r["LastPO"]  != DBNull.Value)
                {
                    s.LastPO = Convert.ToDateTime(r["LastPO"]);
                    s.DaysSinceLastPO = (now - s.LastPO.Value).Days;
                }
                else s.DaysSinceLastPO = 999;

                if (s.POCount > 0) s.AveragePOValue = s.LifetimeSpend / s.POCount;
                if (s.LastYearSpend > 0)
                    s.SpendGrowthPercent = ((s.YtdSpend - s.LastYearSpend) / s.LastYearSpend) * 100;

                cards.Add(s);
            }

            // Score suppliers
            ScoreSuppliers(cards);
            foreach (var s in cards)
            {
                s.Tier = ClassifySupplierTier(s);
                BuildSupplierSignals(s);
            }

            var ranked = cards
                .OrderByDescending(s => s.TotalScore)
                .ThenByDescending(s => s.LifetimeSpend)
                .ToList();
            for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;

            sdp.All = ranked;
            sdp.TierCounts = ranked.GroupBy(s => s.Tier).ToDictionary(g => g.Key, g => g.Count());
            sdp.TotalLifetimeSpend = ranked.Sum(s => s.LifetimeSpend);
            sdp.TotalYtdSpend = ranked.Sum(s => s.YtdSpend);
            sdp.StrategicSupplierCount = ranked.Count(s => s.Tier == "Strategic");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building supplier data platform");
        }
        return sdp;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SCORING HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static void ScoreRecency(List<CustomerScoreCard> cards)
    {
        // Lower days since last activity = higher recency score
        var sorted = cards.OrderBy(c => c.DaysSinceLastActivity).ToList();
        AssignQuintiles(sorted, (c, s) => c.RecencyScore = s);
    }

    private static void ScoreFrequency(List<CustomerScoreCard> cards)
    {
        var sorted = cards.OrderByDescending(c => c.InvoiceCount).ToList();
        AssignQuintiles(sorted, (c, s) => c.FrequencyScore = s);
    }

    private static void ScoreMonetary(List<CustomerScoreCard> cards)
    {
        var sorted = cards.OrderByDescending(c => c.LifetimeRevenue).ToList();
        AssignQuintiles(sorted, (c, s) => c.MonetaryScore = s);
    }

    private static void AssignQuintiles<T>(List<T> sorted, Action<T, int> setter)
    {
        if (sorted.Count == 0) return;
        for (int i = 0; i < sorted.Count; i++)
        {
            var pctFromTop = (double)i / sorted.Count;
            int score = pctFromTop switch
            {
                < 0.20 => 5,
                < 0.40 => 4,
                < 0.60 => 3,
                < 0.80 => 2,
                _      => 1
            };
            setter(sorted[i], score);
        }
    }

    private static string ClassifySegment(CustomerScoreCard c)
    {
        // Kotler-style segmentation
        int r = c.RecencyScore, f = c.FrequencyScore, m = c.MonetaryScore;
        if (r >= 4 && f >= 4 && m >= 4) return "Champions";
        if (r >= 4 && m >= 3) return "Loyal";
        if (r >= 4 && f <= 2) return "New / Promising";
        if (r == 3 && f >= 3) return "Potential Loyalist";
        if (r <= 2 && f >= 4 && m >= 4) return "At Risk";
        if (r <= 2 && f >= 3) return "Needs Attention";
        if (r <= 2 && m >= 4) return "Hibernating Whale";
        if (r == 1 && f <= 2) return "Lost";
        return "Casual";
    }

    private static string ClassifyRating(CustomerScoreCard c)
    {
        if (c.LifetimeRevenue >= 1_000_000) return "Diamond";
        if (c.LifetimeRevenue >= 250_000)   return "Gold";
        if (c.LifetimeRevenue >= 50_000)    return "Silver";
        if (c.LifetimeRevenue >= 10_000)    return "Bronze";
        return "Watch";
    }

    private static string ClassifyHealth(CustomerScoreCard c)
    {
        if (c.GrowthPercent >= 20 && c.DaysSinceLastActivity <= 60) return "healthy";
        if (c.DaysSinceLastActivity > 180 && c.LifetimeRevenue > 25_000) return "critical";
        if (c.DaysSinceLastActivity > 90 || c.GrowthPercent < -25) return "at-risk";
        return "neutral";
    }

    private static void BuildSignals(CustomerScoreCard c)
    {
        if (c.GrowthPercent >= 25) c.SignalsPositive.Add($"+{c.GrowthPercent:N0}% YoY growth");
        if (c.RecencyScore >= 4) c.SignalsPositive.Add("Recent engagement");
        if (c.WinRate >= 50) c.SignalsPositive.Add($"{c.WinRate:N0}% quote win rate");
        if (c.LifetimeRevenue >= 250_000) c.SignalsPositive.Add("High lifetime value");

        if (c.DaysSinceLastActivity > 180) c.SignalsNegative.Add($"No activity in {c.DaysSinceLastActivity} days");
        if (c.GrowthPercent < -25) c.SignalsNegative.Add($"{c.GrowthPercent:N0}% YoY decline");
        if (c.EffortToValueRatio >= 5) c.SignalsNegative.Add("High quoting effort vs revenue");
        if (c.OverdueInvoices > 0) c.SignalsNegative.Add($"{c.OverdueInvoices} overdue invoices");
    }

    private static void ScoreSuppliers(List<SupplierScoreCard> cards)
    {
        // Value score: total spend quintile
        var byValue = cards.OrderByDescending(s => s.LifetimeSpend).ToList();
        AssignQuintiles(byValue, (s, sc) => s.ValueScore = sc);

        // Dependency score: open PO value quintile
        var byDep = cards.OrderByDescending(s => s.OpenPOValue).ToList();
        AssignQuintiles(byDep, (s, sc) => s.DependencyScore = sc);

        // Reliability: PO count + low days since last PO
        var byRel = cards.OrderByDescending(s => s.POCount).ThenBy(s => s.DaysSinceLastPO).ToList();
        AssignQuintiles(byRel, (s, sc) => s.ReliabilityScore = sc);
    }

    private static string ClassifySupplierTier(SupplierScoreCard s)
    {
        if (s.TotalScore >= 13) return "Strategic";
        if (s.TotalScore >= 9)  return "Preferred";
        if (s.TotalScore >= 6)  return "Transactional";
        return "Trial";
    }

    private static void BuildSupplierSignals(SupplierScoreCard s)
    {
        if (s.OpenPOValue >= 50_000) s.Signals.Add($"${s.OpenPOValue:N0} in open POs");
        if (s.POCount >= 20) s.Signals.Add($"{s.POCount} total POs");
        if (s.SpendGrowthPercent >= 25) s.Signals.Add($"+{s.SpendGrowthPercent:N0}% spend growth");
        if (s.DaysSinceLastPO > 365) s.Signals.Add("Inactive for over a year");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CAMPAIGNS
    // ════════════════════════════════════════════════════════════════════════
    public async Task<List<EmailCampaign>> GetCampaignsAsync()
    {
        // Return mock data for now - replace with DB query when table exists
        return new List<EmailCampaign>
        {
            new EmailCampaign
            {
                Id = "1",
                Name = "Q1 Champion Nurture",
                Subject = "Your exclusive early access to new arrivals",
                Audience = "Champions",
                Status = "Sent",
                SentCount = 45,
                OpenRate = 68,
                ClickRate = 24,
                SentAt = new DateTime(2026, 1, 15),
                RecipientCount = 45
            },
            new EmailCampaign
            {
                Id = "2",
                Name = "At-Risk Reactivation",
                Subject = "We miss you — here's 10% off your next order",
                Audience = "At-Risk",
                Status = "Draft",
                SentCount = 0,
                RecipientCount = 12
            },
            new EmailCampaign
            {
                Id = "3",
                Name = "New Product Launch",
                Subject = "Introducing the 2026 Collection",
                Audience = "All Customers",
                Status = "Scheduled",
                SentCount = 0,
                RecipientCount = 156,
                ScheduledAt = new DateTime(2026, 5, 1)
            }
        };
    }

    public async Task<CampaignStats> GetCampaignStatsAsync()
    {
        var campaigns = await GetCampaignsAsync();
        var sent = campaigns.Where(c => c.Status == "Sent");
        return new CampaignStats
        {
            TotalSent = sent.Sum(c => c.SentCount),
            AvgOpenRate = sent.Any() ? (int)sent.Average(c => c.OpenRate) : 0,
            AvgClickRate = sent.Any() ? (int)sent.Average(c => c.ClickRate) : 0
        };
    }

    public async Task SaveCampaignAsync(EmailCampaign campaign)
    {
        // TODO: Persist to database
        _logger.LogInformation("Saving campaign: {Name}", campaign.Name);
        await Task.Delay(100); // Simulate async work
    }

    public async Task SendCampaignAsync(string campaignId)
    {
        // TODO: Implement actual send logic
        _logger.LogInformation("Sending campaign: {Id}", campaignId);
        await Task.Delay(500); // Simulate async work
    }

    public async Task CancelCampaignAsync(string campaignId)
    {
        // TODO: Update status in database
        _logger.LogInformation("Cancelling campaign: {Id}", campaignId);
        await Task.Delay(100);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  STRATEGY DOCUMENT
    // ════════════════════════════════════════════════════════════════════════
    public async Task<MarketingStrategyDoc?> GetStrategyAsync()
    {
        // TODO: Load from database/settings storage
        // Return default/empty doc for now
        return await Task.FromResult(new MarketingStrategyDoc
        {
            IcpIndustries = "Commercial property developers, Architects, Electrical contractors, Retail chains",
            IcpCompanySize = "50-500 employees, $5M-$50M annual revenue",
            IcpPainPoints = "Long lead times, inconsistent quality, lack of technical support, compliance uncertainty",
            IcpBuyingTriggers = "New construction projects, refurbishment cycles, sustainability mandates",
            ValueProposition = "Australian-designed project lighting with guaranteed compliance, 5-day local delivery, and dedicated technical support",
            Differentiators = "Local stock holding, NATA-certified testing, custom design capability, 10-year warranty",
            PositioningStatement = "For Australian commercial developers who can't afford delays, Techlight is the project lighting partner that combines global manufacturing scale with local delivery speed and compliance certainty.",
            Q1Initiatives = "Launch Champion customer program\nImplement automated quote follow-up\nAttend DesignBUILD expo",
            Q2Initiatives = "Roll out email nurture sequences\nLaunch referral incentive program\nPublish sustainability report",
            Q3Initiatives = "Expand supplier network in SE Asia\nLaunch trade portal for electricians\nImplement NPS tracking",
            Q4Initiatives = "Annual customer review program\n2027 strategy planning\nTeam training and certifications",
            KpiLeadTarget = 120,
            KpiConversionRate = 15,
            KpiCacTarget = 2500,
            KpiNpsTarget = 50
        });
    }

    public async Task SaveStrategyAsync(MarketingStrategyDoc strategy)
    {
        // TODO: Persist to database/settings storage
        _logger.LogInformation("Saving marketing strategy document");
        await Task.Delay(100);
    }
}
