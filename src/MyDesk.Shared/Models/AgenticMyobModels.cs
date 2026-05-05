using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// MYOB Integration & Debt Collection & Bank Reconciliation
// ============================================================================

public class MyobOAuthToken
{
    public int      TokenId         { get; set; }
    public string?  TenantId        { get; set; }
    public string   AccessTokenEnc  { get; set; } = "";
    public string   RefreshTokenEnc { get; set; } = "";
    public DateTime ExpiresAt       { get; set; }
    public string?  Scope           { get; set; }
    public string?  CompanyFileUri  { get; set; }
    public string?  CompanyFileName { get; set; }
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class MyobSyncLogEntry
{
    public int      SyncLogId    { get; set; }
    public string   EntityType   { get; set; } = ""; // Invoice/PO/Customer/Payment
    public int      EntityId     { get; set; }
    public string   Direction    { get; set; } = "Push"; // Push/Pull
    public string?  ExternalId   { get; set; }
    public string   Status       { get; set; } = "Pending";
    public int?     StatusCode   { get; set; }
    public string?  ErrorMessage { get; set; }
    public string?  Payload      { get; set; }
    public int      AttemptCount { get; set; } = 1;
    public DateTime SyncedAt     { get; set; }
}

public class MyobWebhookEvent
{
    public int      EventId     { get; set; }
    public string   EventType   { get; set; } = "";
    public string?  EntityUri   { get; set; }
    public string   RawBody     { get; set; } = "";
    public bool     Processed   { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime ReceivedAt  { get; set; }
}

public class ReminderTemplate
{
    public int      TemplateId     { get; set; }
    public string   TemplateKey    { get; set; } = "";
    public string   Subject        { get; set; } = "";
    public string   BodyHtml       { get; set; } = "";
    public string   Channel        { get; set; } = "Email";
    public bool     IncludePayLink { get; set; } = true;
    public bool     IsActive       { get; set; } = true;
    public DateTime CreatedAt      { get; set; }
    public DateTime UpdatedAt      { get; set; }
}

public class DebtCollectionRule
{
    public int      RuleId             { get; set; }
    public string   Name               { get; set; } = "";
    public int?     AppliesToCompanyId { get; set; }
    public int      MinDaysLate        { get; set; }
    public int      MaxDaysLate        { get; set; } = 7;
    public decimal? MinAmount          { get; set; }
    public decimal? MaxAmount          { get; set; }
    public int      TemplateId         { get; set; }
    public int      IntervalDays       { get; set; } = 3;
    public bool     AutoSend           { get; set; }
    public bool     EscalateToAdmin    { get; set; }
    public bool     IsActive           { get; set; } = true;
    public int      Priority           { get; set; } = 100;
    public DateTime CreatedAt          { get; set; }
    public DateTime UpdatedAt          { get; set; }
}

public class DebtCollectionLogEntry
{
    public int      LogId       { get; set; }
    public int      InvoiceId   { get; set; }
    public int?     CompanyId   { get; set; }
    public int?     RuleId      { get; set; }
    public string?  TemplateKey { get; set; }
    public string   Channel     { get; set; } = "Email";
    public string?  ToAddress   { get; set; }
    public string   Status      { get; set; } = "Drafted";
    public string?  Notes       { get; set; }
    public string?  SentBy      { get; set; }
    public DateTime SentAt      { get; set; }
}

/// <summary>
/// View model for the aging-bucket dashboard.
/// </summary>
public class AgingBucketRow
{
    public int      InvoiceId      { get; set; }
    public string?  InvoiceNumber  { get; set; }
    public int?     CompanyId      { get; set; }
    public string?  CompanyName    { get; set; }
    public string?  ContactEmail   { get; set; }
    public DateTime InvoiceDate    { get; set; }
    public DateTime? DueDate       { get; set; }
    public int      DaysLate       { get; set; }
    public decimal  Amount         { get; set; }
    public decimal  AmountOutstanding { get; set; }
    public string   Bucket         { get; set; } = "current"; // current/0-7/8-14/15-30/30+
    public DateTime? LastReminderAt { get; set; }
    public int      ReminderCount  { get; set; }
}

public class BankFeedConnection
{
    public int      ConnectionId    { get; set; }
    public string   Provider        { get; set; } = "Mock"; // Basiq/Akahu/Manual/Mock
    public string?  ProviderUserId  { get; set; }
    public string?  Institution     { get; set; }
    public string?  AccountId       { get; set; }
    public string?  AccountName     { get; set; }
    public string?  AccessTokenEnc  { get; set; }
    public DateTime? ExpiresAt      { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime? LastSyncedAt   { get; set; }
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class BankFeedTransaction
{
    public int      TransactionId    { get; set; }
    public int      ConnectionId     { get; set; }
    public string   ExternalId       { get; set; } = "";
    public DateTime TransactionDate  { get; set; }
    public decimal  Amount           { get; set; }
    public string   Direction        { get; set; } = "Credit"; // Credit/Debit
    public string?  Description      { get; set; }
    public string?  Reference        { get; set; }
    public string?  Counterparty     { get; set; }
    public decimal? Balance          { get; set; }
    public string?  Category         { get; set; }
    public string   MatchStatus      { get; set; } = "Unmatched";
    public int?     MatchedInvoiceId { get; set; }
    public int?     MatchedExpenseId { get; set; }
    public double?  Confidence       { get; set; }
    public DateTime CreatedAt        { get; set; }
}
