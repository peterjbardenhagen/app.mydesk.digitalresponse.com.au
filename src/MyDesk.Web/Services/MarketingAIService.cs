using System.Text;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Marketing-focused AI helper.
/// Wraps AzureAIService with Techlight-aware system prompts and data context.
/// </summary>
public class MarketingAIService
{
    private readonly AzureAIService _ai;
    private readonly MarketingDataService _data;
    private readonly ILogger<MarketingAIService> _logger;

    public MarketingAIService(
        AzureAIService ai,
        MarketingDataService data,
        ILogger<MarketingAIService> logger)
    {
        _ai = ai;
        _data = data;
        _logger = logger;
    }

    public bool IsConfigured => _ai.IsConfigured;

    public AzureChatMessage BuildSystemPrompt(string companyName = "Techlight") => AzureChatMessage.System(
        $@"You are a senior B2B marketing strategist for {companyName}, an Australian project lighting specialist.
You help the executive team build customer profiles, supplier profiles, marketing strategies, and outreach campaigns.
You have access to real data: customer RFM scores, revenue, quotes, invoices, purchase orders, supplier spend, and regions.

Guidelines:
- Be specific. Reference real data from the context when provided.
- Frame answers around actionable next steps.
- When asked for target lists (e.g. 'Top 50 customers'), provide reasoned criteria + a numbered list.
- Use Australian English.
- Output structured markdown (bullets, headings, tables) when helpful.");

    /// <summary>
    /// Builds a concise data-context message the AI can reason over.
    /// </summary>
    public async Task<AzureChatMessage> BuildDataContextAsync()
    {
        var sb = new StringBuilder();
        try
        {
            var cdp = await _data.GetCustomerDataAsync();
            var sdp = await _data.GetSupplierDataAsync();

            sb.AppendLine("## Customer data platform snapshot");
            sb.AppendLine($"- Total customers analysed: {cdp.All.Count}");
            sb.AppendLine($"- Total lifetime revenue: ${cdp.TotalLifetimeRevenue:N0}");
            sb.AppendLine($"- YTD revenue: ${cdp.TotalYtdRevenue:N0}");
            sb.AppendLine($"- Top 10% of customers generate {cdp.Top10PercentRevenueShare}% of revenue");
            sb.AppendLine("- Segment distribution:");
            foreach (var kv in cdp.SegmentCounts.OrderByDescending(k => k.Value))
                sb.AppendLine($"  - {kv.Key}: {kv.Value}");

            sb.AppendLine();
            sb.AppendLine("### Top 10 customers by score");
            foreach (var c in cdp.All.Take(10))
            {
                sb.AppendLine($"- {c.CompanyName} · {c.Segment} · {c.Rating} · YTD ${c.YtdRevenue:N0} · score {c.TotalScore}/15 · growth {c.GrowthPercent:N0}%");
            }

            sb.AppendLine();
            sb.AppendLine("## Supplier data platform snapshot");
            sb.AppendLine($"- Suppliers analysed: {sdp.All.Count}");
            sb.AppendLine($"- Total lifetime spend: ${sdp.TotalLifetimeSpend:N0}");
            sb.AppendLine($"- YTD spend: ${sdp.TotalYtdSpend:N0}");
            sb.AppendLine($"- Strategic suppliers: {sdp.StrategicSupplierCount}");
            sb.AppendLine();
            sb.AppendLine("### Top 10 suppliers by score");
            foreach (var s in sdp.All.Take(10))
            {
                var region = string.IsNullOrEmpty(s.Region) ? "Unknown" : s.Region;
                sb.AppendLine($"- {s.CompanyName} ({region}) · {s.Tier} · Lifetime ${s.LifetimeSpend:N0} · score {s.TotalScore}/15 · {s.POCount} POs");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building marketing AI context");
            sb.AppendLine("[Data context unavailable — proceeding without live data.]");
        }
        return AzureChatMessage.System($"Current business data context:\n{sb}");
    }

    // ── Built-in prompts for common marketing tasks ──────────────────────────
    public static class Prompts
    {
        public const string IdealCustomerProfile =
            "Using the customer data above, build our ideal customer profile. " +
            "Include: industry characteristics, project size, decision-maker roles, " +
            "purchasing patterns, common signals of a high-LTV customer, and red flags " +
            "that indicate a 'wasting time' prospect. Output as a markdown profile document.";

        public const string IdealSupplierProfile =
            "Using the supplier data above, build our ideal supplier partner profile. " +
            "Include: regional preference (Asia-Pacific), reliability patterns, spend tier, " +
            "characteristics of our strongest strategic partners, and signals of risk. " +
            "Output as a markdown profile document.";

        public const string MarketingPlan =
            "Produce a concise 2026 marketing plan for Techlight covering: " +
            "1) Executive summary · 2) Ideal customer profile · 3) Target markets & channels · " +
            "4) Positioning & value proposition · 5) Top 3 quarterly initiatives · " +
            "6) KPIs and targets. Use the data provided.";

        public const string Top50Customers =
            "Based on the customer data, produce the top 50 customer-type targets in Australia " +
            "that Techlight should actively pursue in 2026. Rank 1-50 in a table with columns: " +
            "Rank, Target Profile, Industry/Use-Case, Approximate Project Size, Rationale. " +
            "Group suggestions if individual names aren't available from the data; otherwise name specific existing champions and lookalike prospects.";

        public const string Top50Suppliers =
            "Based on supplier data, recommend the top 50 Asia-Pacific suppliers/manufacturing " +
            "partners that would complement Techlight's project lighting business. " +
            "Output as a table: Rank, Supplier Profile, Region/Country, Specialty, Why a fit. " +
            "Prioritise lighting component, electronics, optics, and logistics partners.";
    }

    /// <summary>
    /// Ask a marketing question and get an AI-powered response with data context.
    /// </summary>
    public async Task<string> AskMarketingQuestionAsync(string question)
    {
        if (!_ai.IsConfigured)
        {
            return "AI service is not configured. Please check Azure OpenAI settings.";
        }

        try
        {
            var messages = new List<AzureChatMessage>
            {
                BuildSystemPrompt(),
                await BuildDataContextAsync(),
                AzureChatMessage.User(question)
            };

            var response = await _ai.ChatAsync(messages);
            return response?.Content ?? "No response from AI.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking marketing question");
            return $"Sorry, I encountered an error: {ex.Message}";
        }
    }
}
