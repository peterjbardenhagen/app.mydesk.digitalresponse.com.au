using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ValuationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ValuationService> _logger;

    public ValuationService(DatabaseService db, ILogger<ValuationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ValuationSnapshot>> GetSnapshotsAsync(int take = 12) =>
        (await _db.QueryAsync<ValuationSnapshot>(
            "SELECT TOP " + take + " * FROM ValuationSnapshots ORDER BY SnapshotDate DESC")).ToList();

    public async Task<ValuationSnapshot?> GetLatestAsync()
    {
        var s = await _db.QueryFirstOrDefaultAsync<ValuationSnapshot>(
            "SELECT TOP 1 * FROM ValuationSnapshots ORDER BY SnapshotDate DESC, SnapshotId DESC");
        if (s != null)
        {
            s.Inputs = (await _db.QueryAsync<ValuationInput>(
                "SELECT * FROM ValuationInputs WHERE SnapshotId=@Id ORDER BY SortOrder, InputId",
                new { Id = s.SnapshotId })).ToList();
        }
        return s;
    }

    public async Task<List<IndustryMultiple>> GetIndustryMultiplesAsync() =>
        (await _db.QueryAsync<IndustryMultiple>(
            "SELECT * FROM IndustryMultiples ORDER BY DisplayName")).ToList();

    public async Task<int> SaveSnapshotAsync(ValuationSnapshot s)
    {
        const string sql = @"INSERT INTO ValuationSnapshots
            (SnapshotDate, Period, PeriodStart, PeriodEnd,
             Revenue, CostOfGoodsSold, GrossProfit, OperatingExpenses, EBITDA, EBITDAMargin,
             Depreciation, Amortisation, Interest, Tax, NetProfit,
             TotalAssets, TotalLiabilities, NetAssets, Cash, Debt,
             IndustryMultiple, ValuationMultiple, ValuationDcf, ValuationAssetBased,
             ValuationLow, ValuationHigh, ValuationMidpoint,
             DataCompleteness, Confidence, IndustryClassification,
             DataSourceMyob, DataSourceBank, DataSourceManual, Notes, CreatedBy)
            VALUES (@SnapshotDate, @Period, @PeriodStart, @PeriodEnd,
             @Revenue, @CostOfGoodsSold, @GrossProfit, @OperatingExpenses, @EBITDA, @EBITDAMargin,
             @Depreciation, @Amortisation, @Interest, @Tax, @NetProfit,
             @TotalAssets, @TotalLiabilities, @NetAssets, @Cash, @Debt,
             @IndustryMultiple, @ValuationMultiple, @ValuationDcf, @ValuationAssetBased,
             @ValuationLow, @ValuationHigh, @ValuationMidpoint,
             @DataCompleteness, @Confidence, @IndustryClassification,
             @DataSourceMyob, @DataSourceBank, @DataSourceManual, @Notes, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var id = await _db.ExecuteScalarAsync<int>(sql, s);
        s.SnapshotId = id;

        foreach (var input in s.Inputs)
        {
            input.SnapshotId = id;
            await _db.ExecuteObjAsync(@"INSERT INTO ValuationInputs
                (SnapshotId, LineItem, Category, Amount, Source, SortOrder, IsAddBack, Notes)
                VALUES (@SnapshotId, @LineItem, @Category, @Amount, @Source, @SortOrder, @IsAddBack, @Notes)", input);
        }
        return id;
    }

    public async Task DeleteSnapshotAsync(int id) =>
        await _db.ExecuteObjAsync("DELETE FROM ValuationSnapshots WHERE SnapshotId = @Id", new { Id = id });

    /// <summary>
    /// Builds a draft snapshot from whatever data sources are connected.
    /// When MYOB / Bank integrations are not yet connected this falls back to
    /// the most recent manual snapshot or empty values for the user to fill.
    /// </summary>
    public async Task<ValuationSnapshot> BuildDraftSnapshotAsync(string industryKey, IndustryMultiple? industry = null)
    {
        industry ??= (await GetIndustryMultiplesAsync())
            .FirstOrDefault(i => i.IndustryKey == industryKey);

        var prior = await GetLatestAsync();

        var snapshot = new ValuationSnapshot
        {
            SnapshotDate           = DateTime.Today,
            Period                 = "TTM",
            PeriodStart            = DateTime.Today.AddYears(-1),
            PeriodEnd              = DateTime.Today,
            IndustryClassification = industry?.DisplayName ?? "Other / Generic",
            IndustryMultiple       = industry?.EbitdaMultipleMid,

            // Carry forward last manual values as a starting point
            Revenue                = prior?.Revenue,
            CostOfGoodsSold        = prior?.CostOfGoodsSold,
            OperatingExpenses      = prior?.OperatingExpenses,
            Depreciation           = prior?.Depreciation,
            Amortisation           = prior?.Amortisation,
            Interest               = prior?.Interest,
            Tax                    = prior?.Tax,
            TotalAssets            = prior?.TotalAssets,
            TotalLiabilities       = prior?.TotalLiabilities,
            Cash                   = prior?.Cash,
            Debt                   = prior?.Debt,

            DataSourceMyob         = false,
            DataSourceBank         = false,
            DataSourceManual       = true,
        };

        Recompute(snapshot, industry);
        return snapshot;
    }

    /// <summary>
    /// In-place recomputation of derived figures (EBITDA, valuation range, etc).
    /// Safe to call on every keystroke from the UI.
    /// </summary>
    public static void Recompute(ValuationSnapshot s, IndustryMultiple? industry)
    {
        var revenue = s.Revenue ?? 0;
        var cogs    = s.CostOfGoodsSold ?? 0;
        var opex    = s.OperatingExpenses ?? 0;

        var addBacks = s.Inputs.Where(i => i.IsAddBack).Sum(i => i.Amount);

        s.GrossProfit = revenue - cogs;
        s.EBITDA      = ValuationEngine.ComputeEbitda(revenue, cogs, opex, addBacks);
        s.EBITDAMargin = revenue > 0 ? (double)(s.EBITDA / revenue) : null;

        var ebit = (s.EBITDA ?? 0) - (s.Depreciation ?? 0) - (s.Amortisation ?? 0);
        s.NetProfit = ebit - (s.Interest ?? 0) - (s.Tax ?? 0);
        s.NetAssets = (s.TotalAssets ?? 0) - (s.TotalLiabilities ?? 0);

        if (industry != null && s.EBITDA.HasValue)
        {
            var (low, mid, high) = ValuationEngine.Multiple(s.EBITDA.Value, industry);
            s.ValuationMultiple   = mid;
            s.ValuationLow        = low;
            s.ValuationHigh       = high;
            s.ValuationMidpoint   = mid;

            s.ValuationDcf        = ValuationEngine.Dcf(s.EBITDA.Value);
            s.ValuationAssetBased = ValuationEngine.AssetBased(
                s.NetAssets ?? 0, s.EBITDA.Value, s.EBITDAMargin ?? 0);
        }

        s.DataCompleteness = ValuationEngine.Completeness(s);
        s.Confidence       = ValuationEngine.ConfidenceFromCompleteness(s.DataCompleteness ?? 0);
    }
}
