using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// ISO 27001 (Asset / Risk / Incident), ISO 9001 (CAPA / Training / Maintenance),
// Staff Onboarding & Policy Governance
// ============================================================================

public class AssetRegisterEntry
{
    public int      AssetId          { get; set; }
    public string   AssetTag         { get; set; } = "";
    public string   AssetType        { get; set; } = "";
    public string?  Manufacturer     { get; set; }
    public string?  Model            { get; set; }
    public string?  SerialNumber     { get; set; }
    public string?  IMEI             { get; set; }
    public int?     AssignedToUserId { get; set; }
    public string?  AssignedToName   { get; set; }
    public DateTime? AssignedDate    { get; set; }
    public string   Status           { get; set; } = "Active";
    public string?  Location         { get; set; }
    public DateTime? PurchaseDate    { get; set; }
    public decimal? PurchasePrice    { get; set; }
    public DateTime? WarrantyExpiry  { get; set; }
    public string?  Notes            { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }
}

public class RiskRegisterEntry
{
    public int      RiskId      { get; set; }
    public string   RiskCode    { get; set; } = "";
    public string   Title       { get; set; } = "";
    public string?  Description { get; set; }
    public string?  Category    { get; set; }
    public int      Likelihood  { get; set; } = 1;
    public int      Impact      { get; set; } = 1;
    public int      Score       { get; set; } // computed Likelihood*Impact
    public string?  Treatment   { get; set; }
    public int?     OwnerUserId { get; set; }
    public string?  OwnerName   { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string   Status      { get; set; } = "Open";
    public DateTime CreatedAt   { get; set; }
    public DateTime UpdatedAt   { get; set; }
}

public class SecurityIncident
{
    public int      IncidentId     { get; set; }
    public string   Title          { get; set; } = "";
    public string?  Description    { get; set; }
    public string   Severity       { get; set; } = "Low";
    public string   Status         { get; set; } = "Open";
    public DateTime DetectedAt     { get; set; }
    public DateTime? ResolvedAt    { get; set; }
    public string?  ReportedBy     { get; set; }
    public string?  AssignedTo     { get; set; }
    public string?  RootCause      { get; set; }
    public string?  LessonsLearned { get; set; }
    public int?     RelatedRiskId  { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}

public class CapaItem
{
    public int      CapaId           { get; set; }
    public string   CapaNumber       { get; set; } = "";
    public string   Title            { get; set; } = "";
    public string?  Source           { get; set; }
    public string?  SourceRef        { get; set; }
    public string?  Description      { get; set; }
    public string?  RootCause        { get; set; }
    public string?  CorrectiveAction { get; set; }
    public string?  PreventiveAction { get; set; }
    public int?     OwnerUserId      { get; set; }
    public string?  OwnerName        { get; set; }
    public DateTime? DueDate         { get; set; }
    public string   Status           { get; set; } = "Open";
    public string?  VerifiedBy       { get; set; }
    public DateTime? VerifiedAt      { get; set; }
    public int?     RelatedJobId     { get; set; }
    public int?     RelatedCompanyId { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }
}

public class TrainingMatrixSkill
{
    public int      MatrixId       { get; set; }
    public string   SkillKey       { get; set; } = "";
    public string   SkillName      { get; set; } = "";
    public string?  Category       { get; set; }
    public bool     Mandatory      { get; set; }
    public int?     ValidityMonths { get; set; }
    public string?  Description    { get; set; }
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; }
}

public class TrainingRecord
{
    public int      RecordId       { get; set; }
    public int      UserId         { get; set; }
    public string?  UserName       { get; set; }
    public string   SkillKey       { get; set; } = "";
    public DateTime AcquiredDate   { get; set; }
    public DateTime? ExpiryDate    { get; set; }
    public string?  Issuer         { get; set; }
    public int?     EvidenceFileId { get; set; }
    public string   Status         { get; set; } = "Current"; // Current/Expiring/Expired
    public string?  Notes          { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}

public class MaintenanceRegisterEntry
{
    public int      MaintenanceId   { get; set; }
    public int?     AssetId         { get; set; }
    public string   EquipmentName   { get; set; } = "";
    public string   ScheduleType    { get; set; } = "Service";
    public int      IntervalDays    { get; set; } = 365;
    public DateTime? LastDoneDate   { get; set; }
    public DateTime? NextDueDate    { get; set; }
    public string?  ResponsibleUser { get; set; }
    public string   Status          { get; set; } = "Active";
    public string?  Notes           { get; set; }
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class OnboardingWorkflow
{
    public int      WorkflowId     { get; set; }
    public string   EmployeeName   { get; set; } = "";
    public string?  EmployeeEmail  { get; set; }
    public DateTime? StartDate     { get; set; }
    public string?  Manager        { get; set; }
    public string?  Position       { get; set; }
    public string?  Division       { get; set; }
    public string   Status         { get; set; } = "Draft";
    public bool     TfnCollected   { get; set; }
    public bool     SuperCollected { get; set; }
    public bool     BankCollected  { get; set; }
    public bool     FairWorkAck    { get; set; }
    public bool     ContractSigned { get; set; }
    public string?  MyobEmployeeId { get; set; }
    public string?  CreatedBy      { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}

public class OnboardingTask
{
    public int      TaskId      { get; set; }
    public int      WorkflowId  { get; set; }
    public string   TaskKey     { get; set; } = "";
    public string   Title       { get; set; } = "";
    public string?  AssignedTo  { get; set; }
    public DateTime? DueDate    { get; set; }
    public string   Status      { get; set; } = "Pending";
    public DateTime? CompletedAt{ get; set; }
    public string?  CompletedBy { get; set; }
    public int      SortOrder   { get; set; }
}

public class PolicyDocument
{
    public int      PolicyDocId    { get; set; }
    public string   PolicyKey      { get; set; } = "";
    public string   Title          { get; set; } = "";
    public string   Version        { get; set; } = "v1.0";
    public string?  Category       { get; set; }
    public string?  FilePath       { get; set; }
    public string?  OneDriveItemId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate    { get; set; }
    public int?     OwnerUserId    { get; set; }
    public bool     IsCurrent      { get; set; } = true;
    public bool     Mandatory      { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}

public class PolicyAcknowledgement
{
    public int      AckId          { get; set; }
    public int      PolicyDocId    { get; set; }
    public int      UserId         { get; set; }
    public string?  UserName       { get; set; }
    public string   VersionAck     { get; set; } = "v1.0";
    public DateTime AcknowledgedAt { get; set; }
    public string?  IpAddress      { get; set; }
}
