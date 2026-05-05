using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// Outlook / Microsoft Graph - Lindy-style autonomous email agent
// ============================================================================

/// <summary>
/// OAuth tokens for both per-user mailboxes ("User") and shared platform
/// mailboxes ("Shared", e.g. info@, sales@, support@).
/// </summary>
public class GraphOAuthToken
{
    public int      TokenId         { get; set; }
    public string   TokenScope      { get; set; } = "User"; // User / Shared
    public int?     UserId          { get; set; }
    public string   UserEmail       { get; set; } = "";
    public string?  DisplayLabel    { get; set; }
    public string   AccessTokenEnc  { get; set; } = "";
    public string   RefreshTokenEnc { get; set; } = "";
    public DateTime ExpiresAt       { get; set; }
    public string?  Scope           { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime? LastSyncedAt   { get; set; }
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

/// <summary>
/// Per-user Outlook + AI preferences. Edited from the user's own Settings page,
/// not from Platform Settings. Mirrors Lindy AI's autonomous-agent feature set.
/// </summary>
public class UserOutlookSettings
{
    public int      SettingId                    { get; set; }
    public int      UserId                       { get; set; }

    /// <summary>Master switch for the per-user agent.</summary>
    public bool     IsEnabled                    { get; set; }

    /// <summary>AI categorises every incoming email (urgent / financial / general / informational).</summary>
    public bool     AutoCategorise               { get; set; } = true;

    /// <summary>Auto-create one Outlook folder per Company in MyDesk.</summary>
    public bool     AutoCreateCompanyFolders     { get; set; } = true;

    /// <summary>Move emails from known Contacts into the matching Company folder.</summary>
    public bool     AutoFileContactEmails        { get; set; } = true;

    /// <summary>Filed emails stay unread in Outlook until the user reads them.</summary>
    public bool     KeepFiledEmailsUnread        { get; set; } = true;

    /// <summary>Use OneDrive RAG context when drafting auto-replies.</summary>
    public bool     UseOneDriveContextForReplies { get; set; }

    /// <summary>Drop AI-drafted replies into the user's Drafts folder.</summary>
    public bool     AutoDraftReplies             { get; set; }

    /// <summary>Run quote/invoice/expense extraction on attachments automatically.</summary>
    public bool     AutoExtractAttachments       { get; set; } = true;

    /// <summary>Enable the daily executive digest email.</summary>
    public bool     DigestEnabled                { get; set; } = true;

    public TimeSpan? DigestSendTimeUtc           { get; set; } = new TimeSpan(21, 0, 0);

    public string?  SignatureHtml                { get; set; }

    public DateTime CreatedAt                    { get; set; }
    public DateTime UpdatedAt                    { get; set; }
}

/// <summary>
/// Maps a MyDesk Company to the Outlook folder created in a specific user's mailbox.
/// </summary>
public class UserCompanyMailFolder
{
    public int      FolderMapId     { get; set; }
    public int      UserId          { get; set; }
    public int      CompanyId       { get; set; }
    public string   OutlookFolderId { get; set; } = "";
    public string   FolderName      { get; set; } = "";
    public DateTime CreatedAt       { get; set; }
}

public class EmailScore
{
    public int      ScoreId        { get; set; }
    public string   GraphMessageId { get; set; } = "";
    public string?  ConversationId { get; set; }
    public string?  UserEmail      { get; set; }
    public string?  FromAddress    { get; set; }
    public string?  Subject        { get; set; }
    public DateTime ReceivedAt     { get; set; }
    public double?  SentimentScore { get; set; }
    public double?  FinancialScore { get; set; }
    public double?  UrgencyScore   { get; set; }
    public double?  PriorityScore  { get; set; }
    public string?  Category       { get; set; } // Urgent / Financial / General / Informational
    public bool     HasAttachment  { get; set; }
    public DateTime ProcessedAt    { get; set; }
}

public class EmailDraft
{
    public int      DraftId         { get; set; }
    public string?  SourceMessageId { get; set; }
    public string   ToAddress       { get; set; } = "";
    public string?  Subject         { get; set; }
    public string   BodyHtml        { get; set; } = "";
    public string?  ContextSnippet  { get; set; }
    public string   Status          { get; set; } = "Draft"; // Draft/Approved/Sent/Discarded
    public string   CreatedBy       { get; set; } = "AI";
    public string?  ApprovedBy      { get; set; }
    public DateTime? SentAt         { get; set; }
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class ExtractedDocumentRecord
{
    public int      ExtractionId     { get; set; }
    public string   SourceType       { get; set; } = "Upload"; // Email/Upload/OneDrive
    public string?  SourceRef        { get; set; }
    public string?  FileName         { get; set; }
    public string?  ContentType      { get; set; }
    public string   Strategy         { get; set; } = "PdfPig";
    public string?  DetectedDocType  { get; set; }
    public string?  ExtractedJson    { get; set; }
    public double?  ConfidenceScore  { get; set; }
    public bool?    AuditPassed      { get; set; }
    public string?  Discrepancies    { get; set; }
    public string?  StagedEntityType { get; set; }
    public int?     StagedEntityId   { get; set; }
    public string   Status           { get; set; } = "Extracted";
    public DateTime CreatedAt        { get; set; }
    public string?  ApprovedBy       { get; set; }
    public DateTime? ApprovedAt      { get; set; }
}
