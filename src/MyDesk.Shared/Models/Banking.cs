namespace MyDesk.Shared.Models;

/// <summary>
/// A bank statement uploaded by the accounts team. Header / metadata only —
/// individual debits and credits live in <see cref="BankTransaction"/>.
///
/// In future this gets populated automatically by a bank-feed integration; for
/// now it is created manually on the <c>/banking/upload</c> page from a CSV
/// or PDF statement export.
/// </summary>
public class BankStatement
{
    public int BankStatementId { get; set; }
    public Guid TenantId { get; set; }

    public string AccountName  { get; set; } = string.Empty;   // e.g. "CBA Business Account"
    public string? Bsb         { get; set; }                   // 6 digit BSB for AU accounts
    public string? AccountNumber { get; set; }                 // last 4 typically
    public string Currency     { get; set; } = "AUD";

    public DateTime FromDate   { get; set; }
    public DateTime ToDate     { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public string? FileName    { get; set; }                   // original upload filename
    public string? FilePath    { get; set; }                   // server-side path or blob ref
    public string  Source      { get; set; } = "manual-csv";   // "manual-csv" | "manual-pdf" | "integration"

    public int  TransactionCount  { get; set; }                // de-normalised count for list views
    public int  ReconciledCount   { get; set; }
    public bool IsFullyReconciled => TransactionCount > 0 && ReconciledCount >= TransactionCount;

    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public string?  UploadedBy { get; set; }
}

/// <summary>
/// One line on a bank statement. Either <see cref="Debit"/> or <see cref="Credit"/>
/// is populated (the other is zero). Once matched to an Invoice / Expense / etc.
/// the <see cref="MatchedEntityType"/> + <see cref="MatchedEntityId"/> identify it
/// and <see cref="Reconciled"/> flips to true.
/// </summary>
public class BankTransaction
{
    public int  BankTransactionId { get; set; }
    public int  BankStatementId   { get; set; }
    public Guid TenantId          { get; set; }

    public DateTime TransactionDate { get; set; }
    public string  Description      { get; set; } = string.Empty;
    public string? Reference        { get; set; }   // bank-supplied ref / cheque no / payee

    public decimal Debit   { get; set; }            // money out
    public decimal Credit  { get; set; }            // money in
    public decimal Balance { get; set; }            // running balance after this txn (if statement provides)

    /// <summary>
    /// One of: <c>Invoice</c>, <c>Expense</c>, <c>PurchaseOrder</c>, <c>Quote</c>, <c>Other</c>.
    /// Null when not yet matched.
    /// </summary>
    public string? MatchedEntityType { get; set; }
    public int?    MatchedEntityId   { get; set; }

    public bool    Reconciled        { get; set; }
    public string? Notes             { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReconciledAt { get; set; }
    public string?  ReconciledBy   { get; set; }
}

/// <summary>Aggregate counts shown on the Banking landing page.</summary>
public record BankingSummary(
    int    StatementCount,
    int    TransactionCount,
    int    ReconciledCount,
    int    UnreconciledCount,
    decimal TotalCreditsAud,
    decimal TotalDebitsAud);
