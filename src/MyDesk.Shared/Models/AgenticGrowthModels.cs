using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// Growth Modules - ABN/ASIC Trust, Grants Hunter, Online Reputation
// ============================================================================

public class AbnLookupResult
{
    public int      AbnLookupId   { get; set; }
    public string   Abn           { get; set; } = "";
    public string?  EntityName    { get; set; }
    public string?  EntityType    { get; set; }
    public bool?    GstRegistered { get; set; }
    public string?  AbnStatus     { get; set; }
    public string?  State         { get; set; }
    public string?  Postcode      { get; set; }
    public string?  AsicStatus    { get; set; }
    public int?     TrustScore    { get; set; }
    public string?  TrustBand     { get; set; } // Green/Amber/Red
    public DateTime LastCheckedAt { get; set; }
    public string?  RawResponse   { get; set; }
}

public class GrantOpportunity
{
    public int      GrantId            { get; set; }
    public string?  ExternalId         { get; set; }
    public string   Title              { get; set; } = "";
    public string?  Agency             { get; set; }
    public string?  Description        { get; set; }
    public string?  EligibilitySummary { get; set; }
    public decimal? AmountMin          { get; set; }
    public decimal? AmountMax          { get; set; }
    public string?  State              { get; set; }
    public string?  Industry           { get; set; }
    public DateTime? OpenDate          { get; set; }
    public DateTime? CloseDate         { get; set; }
    public string?  SourceUrl          { get; set; }
    public double?  EligibilityScore   { get; set; }
    public string   Status             { get; set; } = "Open";
    public DateTime CreatedAt          { get; set; }
    public DateTime UpdatedAt          { get; set; }
}

public class GrantApplication
{
    public int      ApplicationId { get; set; }
    public int      GrantId       { get; set; }
    public string?  ApplicantName { get; set; }
    public string?  DraftBody     { get; set; }
    public string   Status        { get; set; } = "Draft";
    public DateTime? SubmittedAt  { get; set; }
    public string?  OutcomeNote   { get; set; }
    public decimal? AmountAwarded { get; set; }
    public DateTime CreatedAt     { get; set; }
    public DateTime UpdatedAt     { get; set; }
}

public class OnlineReview
{
    public int      ReviewId       { get; set; }
    public string   Source         { get; set; } = "Google";
    public string?  ExternalId     { get; set; }
    public int?     Rating         { get; set; }
    public string?  ReviewerName   { get; set; }
    public string?  ReviewText     { get; set; }
    public double?  SentimentScore { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public string?  ReplyDraft     { get; set; }
    public string?  ReplyStatus    { get; set; }
    public DateTime? ReplyPostedAt { get; set; }
    public DateTime CreatedAt      { get; set; }
}
