using System;
using System.Collections.Generic;

namespace MyDesk.Shared.Models;

// ============================================================================
// Strategy area: Business Goals (1-5 year executive roadmap) + Valuation
// ============================================================================

public class BusinessGoal
{
    public int      GoalId            { get; set; }
    public string   Title             { get; set; } = "";
    public string?  Description       { get; set; }
    public string   Category          { get; set; } = "Sales";
    public int      HorizonYears      { get; set; } = 1;
    public DateTime? TargetDate       { get; set; }
    public DateTime StartDate         { get; set; } = DateTime.Today;
    public int?     OwnerUserId       { get; set; }
    public string?  OwnerName         { get; set; }
    public string   Priority          { get; set; } = "Medium";
    public string   Status            { get; set; } = "Active";
    public int      ProgressPercent   { get; set; }
    public string   Confidence        { get; set; } = "Medium";
    public string?  SuccessCriteria   { get; set; }
    public bool     AiCoachingEnabled { get; set; } = true;
    public string?  CreatedBy         { get; set; }
    public DateTime CreatedAt         { get; set; }
    public DateTime UpdatedAt         { get; set; }

    public List<BusinessGoalKpi> Kpis { get; set; } = new();

    // Convenience for the UI
    public int      DaysRemaining =>
        TargetDate.HasValue ? (int)(TargetDate.Value - DateTime.Today).TotalDays : 0;

    public bool IsOverdue => TargetDate.HasValue && TargetDate.Value < DateTime.Today && Status != "Achieved" && Status != "Cancelled";

    public static IReadOnlyList<string> Categories { get; } = new[]
    {
        "Sales", "Growth", "Operational", "Financial", "People",
        "Compliance", "Strategic", "MergerAcquisition", "IPO", "Exit"
    };

    public static IReadOnlyList<string> Statuses { get; } = new[]
    {
        "Draft", "Active", "OnTrack", "AtRisk", "OffTrack", "Achieved", "Cancelled"
    };
}

public class BusinessGoalKpi
{
    public int      KpiId         { get; set; }
    public int      GoalId        { get; set; }
    public string   Name          { get; set; } = "";
    public string   MetricType    { get; set; } = "Number"; // Number/Currency/Percent/Ratio/Boolean
    public string?  DataSource    { get; set; }
    public string?  DataKey       { get; set; }
    public decimal? BaselineValue { get; set; }
    public decimal  TargetValue   { get; set; }
    public decimal? CurrentValue  { get; set; }
    public string?  Unit          { get; set; }
    public string   Direction     { get; set; } = "Increase";
    public string   UpdateCadence { get; set; } = "Monthly";
    public DateTime? LastUpdated  { get; set; }
    public string   Status        { get; set; } = "OnTrack";
    public DateTime CreatedAt     { get; set; }
    public DateTime UpdatedAt     { get; set; }

    /// <summary>0..100 progress against target accounting for direction.</summary>
    public int ProgressPercent
    {
        get
        {
            if (CurrentValue is null || TargetValue == 0) return 0;
            var baseline = BaselineValue ?? 0m;
            var span = TargetValue - baseline;
            if (span == 0) return 100;
            var moved = (CurrentValue.Value - baseline) / span;
            var pct = (int)Math.Round(Math.Clamp((decimal)moved, 0m, 1m) * 100);
            return pct;
        }
    }
}

public class BusinessRoadmap
{
    public int      RoadmapId   { get; set; }
    public int?     GoalId      { get; set; }
    public string   Title       { get; set; } = "";
    public string?  Description { get; set; }
    public int?     FromYear    { get; set; }
    public int?     ToYear      { get; set; }
    public string?  FilePath    { get; set; }
    public string?  FileName    { get; set; }
    public string?  ContentType { get; set; }
    public long?    SizeBytes   { get; set; }
    public string?  UploadedBy  { get; set; }
    public DateTime UploadedAt  { get; set; }
    public bool     IsCurrent   { get; set; } = true;
    public string?  Notes       { get; set; }
}

public class GoalCheckIn
{
    public int      CheckInId       { get; set; }
    public int      GoalId          { get; set; }
    public DateTime CheckInDate     { get; set; }
    public string   Source          { get; set; } = "Human"; // Human / AI
    public string?  AuthorName      { get; set; }
    public int?     ProgressPercent { get; set; }
    public string?  Status          { get; set; }
    public string?  Summary         { get; set; }
    public string?  Recommendation  { get; set; }
    public string?  DiscrepancyJson { get; set; }
}
