using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// OneDrive RAG Index + Privacy & Retention (Light Purview)
// ============================================================================

public class OneDriveIndexEntry
{
    public int      IndexId          { get; set; }
    public string   DriveItemId      { get; set; } = "";
    public string?  ParentFolder     { get; set; }
    public string   FileName         { get; set; } = "";
    public string?  FilePath         { get; set; }
    public string?  ContentType      { get; set; }
    public long?    SizeBytes        { get; set; }
    public string?  ETag             { get; set; }
    public string?  ContentHash      { get; set; }
    public int?     CompanyId        { get; set; }
    public int?     ProjectId        { get; set; }
    public string?  SensitivityLabel { get; set; }
    public bool     VectorIndexed    { get; set; }
    public DateTime? VectorIndexedAt { get; set; }
    public string?  VectorBackend    { get; set; }
    public string?  VectorRefId      { get; set; }
    public DateTime? LastModified    { get; set; }
    public DateTime IndexedAt        { get; set; }
}

public class IndexSyncJob
{
    public int      JobId          { get; set; }
    public string   JobType        { get; set; } = "DeltaSync";
    public string?  DeltaToken     { get; set; }
    public string   Status         { get; set; } = "Queued";
    public int      ItemsProcessed { get; set; }
    public int      ItemsFailed    { get; set; }
    public string?  ErrorMessage   { get; set; }
    public DateTime? StartedAt     { get; set; }
    public DateTime? CompletedAt   { get; set; }
    public DateTime CreatedAt      { get; set; }
}

public class RetentionPolicy
{
    public int      PolicyId          { get; set; }
    public string   PolicyKey         { get; set; } = "";
    public string   Name              { get; set; } = "";
    public string   EntityType        { get; set; } = "";
    public string   TriggerEvent      { get; set; } = "";
    public int      RetentionYears    { get; set; }
    public string   ActionAfterExpiry { get; set; } = "Archive";
    public string?  LegalBasis        { get; set; }
    public bool     IsActive          { get; set; } = true;
    public DateTime CreatedAt         { get; set; }
    public DateTime UpdatedAt         { get; set; }
}

public class PiiClassification
{
    public int      ClassificationId { get; set; }
    public string   EntityType       { get; set; } = "";
    public string   EntityRef        { get; set; } = "";
    public string?  PiiTypes         { get; set; } // CSV
    public string   SensitivityLabel { get; set; } = "General";
    public double?  ConfidenceScore  { get; set; }
    public DateTime DetectedAt       { get; set; }
    public string?  ReviewedBy       { get; set; }
    public DateTime? ReviewedAt      { get; set; }
}

public class SubjectAccessRequest
{
    public int      RequestId    { get; set; }
    public string   SubjectType  { get; set; } = "Customer";
    public string   SubjectIdent { get; set; } = "";
    public string?  RequestedBy  { get; set; }
    public string   Status       { get; set; } = "Pending";
    public string?  ReportPath   { get; set; }
    public DateTime? DueDate     { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class ComplianceLedgerEntry
{
    public long     LedgerId   { get; set; }
    public string   Actor      { get; set; } = "";
    public string   Action     { get; set; } = "";
    public string   EntityType { get; set; } = "";
    public string   EntityRef  { get; set; } = "";
    public string?  Outcome    { get; set; }
    public string?  Detail     { get; set; }
    public string?  IpAddress  { get; set; }
    public DateTime OccurredAt { get; set; }
}
