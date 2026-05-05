using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDesk.Shared.Models;

// ============================================================================
// Business Valuation models - EBITDA, multiples, DCF, asset-based.
// ============================================================================

public class ValuationSnapshot
{
    public int      SnapshotId          { get; set; }
    public DateTime SnapshotDate        { get; set; } = DateTime.Today;
    public string   Period              { get; set; } = "TTM"; // TTM / FY / YTD / Custom
    public DateTime? PeriodStart        { get; set; }
    public DateTime? PeriodEnd          { get; set; }

    // Income statement
    public decimal? Revenue             { get; set; }
    public decimal? CostOfGoodsSold     { get; set; }
    public decimal? GrossProfit         { get; set; }
    public decimal? OperatingExpenses   { get; set; }
    public decimal? EBITDA              { get; set; }
    public double?  EBITDAMargin        { get; set; }
    public decimal? Depreciation        { get; set; }
    public decimal? Amortisation        { get; set; }
    public decimal? Interest            { get; set; }
    public decimal? Tax                 { get; set; }
    public decimal? NetProfit           { get; set; }

    // Balance sheet
    public decimal? TotalAssets         { get; set; }
    public decimal? TotalLiabilities    { get; set; }
    public decimal? NetAssets           { get; set; }
    public decimal? Cash                { get; set; }
    public decimal? Debt                { get; set; }

    // Valuation methods
    public double?  IndustryMultiple    { get; set; }
    public decimal? ValuationMultiple   { get; set; }
    public decimal? ValuationDcf        { get; set; }
    public decimal? ValuationAssetBased { get; set; }
    public decimal? ValuationLow        { get; set; }
    public decimal? ValuationHigh       { get; set; }
    public decimal? ValuationMidpoint   { get; set; }

    // Quality
    public double?  DataCompleteness    { get; set; }
    public string?  Confidence          { get; set; } // Low / Medium / High
    public string?  IndustryClassification { get; set; }

    // Source flags
    public bool     DataSourceMyob      { get; set; }
    public bool     DataSourceBank      { get; set; }
    public bool     DataSourceManual    { get; set; }
    public string?  Notes               { get; set; }
    public string?  CreatedBy           { get; set; }
    public DateTime CreatedAt           { get; set; }

    public List<ValuationInput> Inputs  { get; set; } = new();
}

public class ValuationInput
{
    public int      InputId    { get; set; }
    public int      SnapshotId { get; set; }
    public string   LineItem   { get; set; } = "";
    public string   Category   { get; set; } = "OpEx"; // Revenue/COGS/OpEx/AddBack/Other
    public decimal  Amount     { get; set; }
    public string?  Source     { get; set; }
    public int      SortOrder  { get; set; }
    public bool     IsAddBack  { get; set; }
    public string?  Notes      { get; set; }
}

public class IndustryMultiple
{
    public int      IndustryId          { get; set; }
    public string   IndustryKey         { get; set; } = "";
    public string   DisplayName         { get; set; } = "";
    public double   EbitdaMultipleLow   { get; set; }
    public double   EbitdaMultipleHigh  { get; set; }
    public double?  RevenueMultipleLow  { get; set; }
    public double?  RevenueMultipleHigh { get; set; }
    public string?  Notes               { get; set; }
    public DateTime UpdatedAt           { get; set; }

    public double EbitdaMultipleMid => (EbitdaMultipleLow + EbitdaMultipleHigh) / 2.0;
}

/// <summary>
/// Pure-C# valuation engine. Given a set of inputs and an industry, produces
/// the valuation range without needing a database call.
/// </summary>
public static class ValuationEngine
{
    /// <summary>Compute EBITDA from inputs. Add-backs are added to Operating Expenses to normalise.</summary>
    public static decimal ComputeEbitda(decimal revenue, decimal cogs, decimal opex, decimal addBacks)
        => revenue - cogs - opex + addBacks;

    /// <summary>
    /// Apply low/mid/high industry multiples to compute a valuation range.
    /// </summary>
    public static (decimal low, decimal mid, decimal high) Multiple(decimal ebitda, IndustryMultiple m)
        => (
            (decimal)m.EbitdaMultipleLow  * ebitda,
            (decimal)m.EbitdaMultipleMid  * ebitda,
            (decimal)m.EbitdaMultipleHigh * ebitda
        );

    /// <summary>
    /// 5-year discounted cash flow with terminal value.
    /// </summary>
    public static decimal Dcf(decimal ebitda, double growthRate = 0.05, double discountRate = 0.12, int years = 5, double terminalGrowth = 0.025)
    {
        decimal pv = 0;
        var cash = ebitda;
        for (var year = 1; year <= years; year++)
        {
            cash *= (decimal)(1 + growthRate);
            pv += cash / (decimal)Math.Pow(1 + discountRate, year);
        }
        // Terminal value: Gordon growth model
        var terminalCash = cash * (decimal)(1 + terminalGrowth);
        var terminalValue = terminalCash / (decimal)(discountRate - terminalGrowth);
        pv += terminalValue / (decimal)Math.Pow(1 + discountRate, years);
        return pv;
    }

    /// <summary>
    /// Asset-based: net assets + a goodwill premium scaled by EBITDA margin.
    /// </summary>
    public static decimal AssetBased(decimal netAssets, decimal ebitda, double ebitdaMargin)
        => netAssets + (decimal)Math.Max(0, ebitdaMargin) * ebitda;

    /// <summary>
    /// Score 0..1 measuring how complete the input data is for confidence reporting.
    /// </summary>
    public static double Completeness(ValuationSnapshot s)
    {
        var checks = new[]
        {
            s.Revenue.HasValue,
            s.CostOfGoodsSold.HasValue,
            s.OperatingExpenses.HasValue,
            s.Depreciation.HasValue,
            s.Amortisation.HasValue,
            s.Interest.HasValue,
            s.Tax.HasValue,
            s.TotalAssets.HasValue,
            s.TotalLiabilities.HasValue,
            s.IndustryMultiple.HasValue,
        };
        return checks.Count(b => b) / (double)checks.Length;
    }

    public static string ConfidenceFromCompleteness(double completeness) =>
        completeness >= 0.85 ? "High" :
        completeness >= 0.55 ? "Medium" : "Low";
}
