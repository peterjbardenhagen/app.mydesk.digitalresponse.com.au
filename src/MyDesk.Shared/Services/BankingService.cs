using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// CRUD + import for the Banking module.
///
/// <para>
/// Schema is created in <see cref="EnsureTablesAsync"/> on startup. Both tables
/// carry a <c>TenantId</c> column so <see cref="TenantIsolationService"/> auto-applies
/// SQL Row-Level Security on the next startup — meaning every query through
/// <c>DatabaseService</c> is automatically scoped to the current tenant.
/// </para>
/// <para>
/// CSV import (<see cref="ImportCsvAsync"/>) supports the common Australian bank
/// CSV shape: Date, Description, Debit (or "Withdrawal"), Credit (or "Deposit"),
/// Balance. Header row is auto-detected.
/// </para>
/// </summary>
public class BankingService
{
    private readonly DatabaseService _db;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly ILogger<BankingService> _logger;

    public BankingService(DatabaseService db, ICurrentTenantAccessor tenant, ILogger<BankingService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task EnsureTablesAsync()
    {
        var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BankStatements')
BEGIN
    CREATE TABLE BankStatements (
        BankStatementId  INT IDENTITY(1,1) PRIMARY KEY,
        TenantId         UNIQUEIDENTIFIER NOT NULL,
        AccountName      NVARCHAR(200) NOT NULL,
        Bsb              NVARCHAR(20)  NULL,
        AccountNumber    NVARCHAR(50)  NULL,
        Currency         NVARCHAR(3)   NOT NULL DEFAULT 'AUD',
        FromDate         DATETIME      NOT NULL,
        ToDate           DATETIME      NOT NULL,
        OpeningBalance   DECIMAL(18,2) NOT NULL DEFAULT 0,
        ClosingBalance   DECIMAL(18,2) NOT NULL DEFAULT 0,
        FileName         NVARCHAR(255) NULL,
        FilePath         NVARCHAR(500) NULL,
        Source           NVARCHAR(50)  NOT NULL DEFAULT 'manual-csv',
        TransactionCount INT           NOT NULL DEFAULT 0,
        ReconciledCount  INT           NOT NULL DEFAULT 0,
        UploadedAt       DATETIME      NOT NULL DEFAULT GETDATE(),
        UploadedBy       NVARCHAR(100) NULL
    );
    CREATE INDEX IX_BankStatements_TenantId ON BankStatements(TenantId);
    CREATE INDEX IX_BankStatements_FromDate ON BankStatements(FromDate DESC);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BankTransactions')
BEGIN
    CREATE TABLE BankTransactions (
        BankTransactionId INT IDENTITY(1,1) PRIMARY KEY,
        BankStatementId   INT NOT NULL,
        TenantId          UNIQUEIDENTIFIER NOT NULL,
        TransactionDate   DATETIME NOT NULL,
        Description       NVARCHAR(500) NOT NULL DEFAULT '',
        Reference         NVARCHAR(200) NULL,
        Debit             DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credit            DECIMAL(18,2) NOT NULL DEFAULT 0,
        Balance           DECIMAL(18,2) NOT NULL DEFAULT 0,
        MatchedEntityType NVARCHAR(50)  NULL,
        MatchedEntityId   INT           NULL,
        Reconciled        BIT NOT NULL DEFAULT 0,
        Notes             NVARCHAR(MAX) NULL,
        CreatedAt         DATETIME NOT NULL DEFAULT GETDATE(),
        ReconciledAt      DATETIME NULL,
        ReconciledBy      NVARCHAR(100) NULL,
        CONSTRAINT FK_BankTransactions_Statement FOREIGN KEY (BankStatementId) REFERENCES BankStatements(BankStatementId) ON DELETE CASCADE
    );
    CREATE INDEX IX_BankTransactions_StatementId ON BankTransactions(BankStatementId);
    CREATE INDEX IX_BankTransactions_TenantId    ON BankTransactions(TenantId);
    CREATE INDEX IX_BankTransactions_Date        ON BankTransactions(TransactionDate DESC);
    CREATE INDEX IX_BankTransactions_Reconciled  ON BankTransactions(Reconciled, TenantId);
END";
        await _db.ExecuteNonQueryAsync(sql);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Statements
    // ──────────────────────────────────────────────────────────────────────

    public async Task<List<MyDesk.Shared.Models.BankStatement>> ListStatementsAsync()
    {
        var dt = await _db.QueryAsync(
            @"SELECT * FROM BankStatements ORDER BY FromDate DESC, BankStatementId DESC");
        return dt.Map(MapStatement);
    }

    public async Task<MyDesk.Shared.Models.BankStatement?> GetStatementAsync(int id)
    {
        var dt = await _db.QueryAsync(
            "SELECT * FROM BankStatements WHERE BankStatementId = @Id",
            new() { ["Id"] = id });
        return dt.Map(MapStatement).FirstOrDefault();
    }

    public async Task<int> CreateStatementAsync(MyDesk.Shared.Models.BankStatement s)
    {
        const string sql = @"
INSERT INTO BankStatements
    (AccountName, Bsb, AccountNumber, Currency, FromDate, ToDate,
     OpeningBalance, ClosingBalance, FileName, FilePath, Source,
     TransactionCount, ReconciledCount, UploadedAt, UploadedBy)
OUTPUT INSERTED.BankStatementId
VALUES
    (@AccountName, @Bsb, @AccountNumber, @Currency, @FromDate, @ToDate,
     @OpeningBalance, @ClosingBalance, @FileName, @FilePath, @Source,
     @TransactionCount, @ReconciledCount, @UploadedAt, @UploadedBy);";

        return await _db.ScalarAsync<int>(sql, new()
        {
            ["AccountName"]     = s.AccountName,
            ["Bsb"]             = (object?)s.Bsb ?? DBNull.Value,
            ["AccountNumber"]   = (object?)s.AccountNumber ?? DBNull.Value,
            ["Currency"]        = s.Currency,
            ["FromDate"]        = s.FromDate,
            ["ToDate"]          = s.ToDate,
            ["OpeningBalance"]  = s.OpeningBalance,
            ["ClosingBalance"]  = s.ClosingBalance,
            ["FileName"]        = (object?)s.FileName ?? DBNull.Value,
            ["FilePath"]        = (object?)s.FilePath ?? DBNull.Value,
            ["Source"]          = s.Source,
            ["TransactionCount"]= s.TransactionCount,
            ["ReconciledCount"] = s.ReconciledCount,
            ["UploadedAt"]      = s.UploadedAt,
            ["UploadedBy"]      = (object?)(s.UploadedBy ?? _tenant.UserCode) ?? DBNull.Value,
        });
    }

    public async Task DeleteStatementAsync(int id)
    {
        // Cascade configured at FK; this is also tenant-filtered by RLS.
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM BankStatements WHERE BankStatementId = @Id",
            new() { ["Id"] = id });
    }

    // ──────────────────────────────────────────────────────────────────────
    // Transactions
    // ──────────────────────────────────────────────────────────────────────

    public async Task<List<MyDesk.Shared.Models.BankTransaction>> ListTransactionsAsync(int statementId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT * FROM BankTransactions WHERE BankStatementId = @Id ORDER BY TransactionDate, BankTransactionId",
            new() { ["Id"] = statementId });
        return dt.Map(MapTransaction);
    }

    /// <summary>
    /// All transactions for the current tenant across all imported statements.
    /// Used by the dashboard Banking slide to build monthly debit/credit trends.
    /// RLS tenant-scopes this automatically.
    /// </summary>
    public async Task<List<MyDesk.Shared.Models.BankTransaction>> GetAllTransactionsAsync(int? limit = null)
    {
        var top = limit.HasValue ? $"TOP ({limit.Value}) " : string.Empty;
        var dt = await _db.QueryAsync(
            $@"SELECT {top}* FROM BankTransactions ORDER BY TransactionDate DESC, BankTransactionId DESC");
        return dt.Map(MapTransaction);
    }

    public async Task<List<MyDesk.Shared.Models.BankTransaction>> ListUnreconciledAsync(int? limit = 200)
    {
        var dt = await _db.QueryAsync(
            $@"SELECT TOP ({limit ?? 200}) * FROM BankTransactions
               WHERE Reconciled = 0 ORDER BY TransactionDate DESC");
        return dt.Map(MapTransaction);
    }

    public async Task<int> AddTransactionAsync(MyDesk.Shared.Models.BankTransaction t)
    {
        const string sql = @"
INSERT INTO BankTransactions
    (BankStatementId, TransactionDate, Description, Reference, Debit, Credit, Balance,
     MatchedEntityType, MatchedEntityId, Reconciled, Notes, CreatedAt, ReconciledAt, ReconciledBy)
OUTPUT INSERTED.BankTransactionId
VALUES
    (@BankStatementId, @TransactionDate, @Description, @Reference, @Debit, @Credit, @Balance,
     @MatchedEntityType, @MatchedEntityId, @Reconciled, @Notes, @CreatedAt, @ReconciledAt, @ReconciledBy);";
        return await _db.ScalarAsync<int>(sql, new()
        {
            ["BankStatementId"]   = t.BankStatementId,
            ["TransactionDate"]   = t.TransactionDate,
            ["Description"]       = t.Description,
            ["Reference"]         = (object?)t.Reference ?? DBNull.Value,
            ["Debit"]             = t.Debit,
            ["Credit"]            = t.Credit,
            ["Balance"]           = t.Balance,
            ["MatchedEntityType"] = (object?)t.MatchedEntityType ?? DBNull.Value,
            ["MatchedEntityId"]   = (object?)t.MatchedEntityId ?? DBNull.Value,
            ["Reconciled"]        = t.Reconciled,
            ["Notes"]             = (object?)t.Notes ?? DBNull.Value,
            ["CreatedAt"]         = t.CreatedAt,
            ["ReconciledAt"]      = (object?)t.ReconciledAt ?? DBNull.Value,
            ["ReconciledBy"]      = (object?)t.ReconciledBy ?? DBNull.Value,
        });
    }

    public async Task ReconcileAsync(int transactionId, string entityType, int? entityId, string? notes = null)
    {
        await _db.ExecuteNonQueryAsync(@"
UPDATE BankTransactions
SET MatchedEntityType = @Type, MatchedEntityId = @Id, Reconciled = 1,
    Notes = COALESCE(@Notes, Notes),
    ReconciledAt = GETDATE(), ReconciledBy = @By
WHERE BankTransactionId = @TxnId;

UPDATE BankStatements
SET ReconciledCount = (
    SELECT COUNT(*) FROM BankTransactions
    WHERE BankStatementId = BankStatements.BankStatementId AND Reconciled = 1)
WHERE BankStatementId = (
    SELECT BankStatementId FROM BankTransactions WHERE BankTransactionId = @TxnId);",
            new()
            {
                ["TxnId"] = transactionId,
                ["Type"]  = entityType,
                ["Id"]    = (object?)entityId ?? DBNull.Value,
                ["Notes"] = (object?)notes ?? DBNull.Value,
                ["By"]    = _tenant.UserCode ?? "system",
            });
    }

    public async Task UnreconcileAsync(int transactionId)
    {
        await _db.ExecuteNonQueryAsync(@"
UPDATE BankTransactions SET MatchedEntityType = NULL, MatchedEntityId = NULL,
    Reconciled = 0, ReconciledAt = NULL, ReconciledBy = NULL
WHERE BankTransactionId = @Id;

UPDATE BankStatements
SET ReconciledCount = (
    SELECT COUNT(*) FROM BankTransactions
    WHERE BankStatementId = BankStatements.BankStatementId AND Reconciled = 1)
WHERE BankStatementId = (
    SELECT BankStatementId FROM BankTransactions WHERE BankTransactionId = @Id);",
            new() { ["Id"] = transactionId });
    }

    // ──────────────────────────────────────────────────────────────────────
    // CSV import
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parse a bank-export CSV (simple format: Date, Description, Debit, Credit, Balance with
    /// optional header row), create a BankStatement, and insert all transactions in order.
    /// Returns (statementId, transactionCount).
    /// </summary>
    public async Task<(int StatementId, int Count)> ImportCsvAsync(
        string csvText,
        string accountName,
        string? bsb = null,
        string? accountNumber = null,
        string fileName = "upload.csv")
    {
        var rows = ParseCsv(csvText);
        if (rows.Count == 0) throw new InvalidOperationException("No transactions found in CSV.");

        var stmt = new MyDesk.Shared.Models.BankStatement
        {
            AccountName    = accountName,
            Bsb            = bsb,
            AccountNumber  = accountNumber,
            Currency       = "AUD",
            FromDate       = rows.Min(r => r.Date),
            ToDate         = rows.Max(r => r.Date),
            OpeningBalance = rows.First().Balance - (rows.First().Credit - rows.First().Debit),
            ClosingBalance = rows.Last().Balance,
            FileName       = fileName,
            Source         = "manual-csv",
            TransactionCount = rows.Count,
            UploadedBy     = _tenant.UserCode,
        };
        var stmtId = await CreateStatementAsync(stmt);

        foreach (var r in rows)
        {
            await AddTransactionAsync(new MyDesk.Shared.Models.BankTransaction
            {
                BankStatementId = stmtId,
                TransactionDate = r.Date,
                Description     = r.Description,
                Reference       = r.Reference,
                Debit           = r.Debit,
                Credit          = r.Credit,
                Balance         = r.Balance,
            });
        }

        return (stmtId, rows.Count);
    }

    private record ParsedRow(DateTime Date, string Description, string? Reference, decimal Debit, decimal Credit, decimal Balance);

    /// <summary>
    /// Lenient CSV parser. Auto-detects header row by looking for "date", "amount" or
    /// "balance" tokens. Splits on commas — values containing commas should be quoted.
    /// </summary>
    private static List<ParsedRow> ParseCsv(string csv)
    {
        var rows = new List<ParsedRow>();
        var lines = csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return rows;

        // Header detection
        int startAt = 0;
        var header = SplitCsvLine(lines[0]).Select(s => s.ToLowerInvariant()).ToArray();
        bool hasHeader = header.Any(h => h.Contains("date")) &&
                         (header.Any(h => h.Contains("debit") || h.Contains("withdrawal") || h.Contains("amount")));

        // Resolve column indexes (defaults match a 5-col debit/credit/balance layout).
        int idxDate = 0, idxDesc = 1, idxDebit = 2, idxCredit = 3, idxBalance = 4, idxRef = -1;
        if (hasHeader)
        {
            startAt = 1;
            for (int i = 0; i < header.Length; i++)
            {
                var h = header[i];
                if (h.Contains("date"))                                   idxDate    = i;
                else if (h.Contains("desc") || h.Contains("narration"))   idxDesc    = i;
                else if (h.Contains("debit") || h.Contains("withdrawal")) idxDebit   = i;
                else if (h.Contains("credit") || h.Contains("deposit"))   idxCredit  = i;
                else if (h.Contains("balance"))                           idxBalance = i;
                else if (h.Contains("ref") || h.Contains("payee"))        idxRef     = i;
            }
        }

        var ci = CultureInfo.GetCultureInfo("en-AU");
        for (int i = startAt; i < lines.Length; i++)
        {
            var fields = SplitCsvLine(lines[i]);
            if (fields.Length <= idxDate) continue;

            if (!TryParseDate(fields[idxDate], out var dt)) continue;

            string desc  = idxDesc    < fields.Length ? fields[idxDesc].Trim()    : "";
            string? rf   = idxRef     >= 0 && idxRef    < fields.Length ? fields[idxRef].Trim() : null;
            decimal dr   = idxDebit   < fields.Length ? ParseDecimal(fields[idxDebit])   : 0m;
            decimal cr   = idxCredit  < fields.Length ? ParseDecimal(fields[idxCredit])  : 0m;
            decimal bal  = idxBalance < fields.Length ? ParseDecimal(fields[idxBalance]) : 0m;

            rows.Add(new ParsedRow(dt, desc, rf, dr, cr, bal));
        }
        return rows.OrderBy(r => r.Date).ToList();
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuote = false;
        foreach (var c in line)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ',' && !inQuote) { fields.Add(cur.ToString()); cur.Clear(); continue; }
            cur.Append(c);
        }
        fields.Add(cur.ToString());
        return fields.ToArray();
    }

    private static bool TryParseDate(string s, out DateTime dt)
    {
        s = s.Trim().Trim('"');
        var formats = new[]
        {
            "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy",
            "d-MMM-yyyy", "dd-MMM-yyyy", "d/MM/yy", "dd/MM/yy"
        };
        return DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
            || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }

    private static decimal ParseDecimal(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        s = s.Trim().Trim('"').Replace("$", "").Replace(",", "").Replace(" ", "");
        if (s.StartsWith("(") && s.EndsWith(")")) s = "-" + s.Trim('(', ')');
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Summary
    // ──────────────────────────────────────────────────────────────────────

    public async Task<BankingSummary> GetSummaryAsync()
    {
        var dt = await _db.QueryAsync(@"
SELECT
    (SELECT COUNT(*) FROM BankStatements)                                AS StmtCount,
    (SELECT COUNT(*) FROM BankTransactions)                              AS TxnCount,
    (SELECT COUNT(*) FROM BankTransactions WHERE Reconciled = 1)         AS RecCount,
    ISNULL((SELECT SUM(Credit) FROM BankTransactions), 0)                AS Credits,
    ISNULL((SELECT SUM(Debit)  FROM BankTransactions), 0)                AS Debits;");
        if (dt.Rows.Count == 0) return new BankingSummary(0,0,0,0,0,0);
        var r = dt.Rows[0];
        var total = Convert.ToInt32(r["TxnCount"]);
        var rec   = Convert.ToInt32(r["RecCount"]);
        return new BankingSummary(
            Convert.ToInt32(r["StmtCount"]),
            total,
            rec,
            total - rec,
            Convert.ToDecimal(r["Credits"]),
            Convert.ToDecimal(r["Debits"]));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Mappers
    // ──────────────────────────────────────────────────────────────────────

    private static MyDesk.Shared.Models.BankStatement MapStatement(DataRow r) => new()
    {
        BankStatementId   = Convert.ToInt32(r["BankStatementId"]),
        TenantId          = r.Table.Columns.Contains("TenantId") && r["TenantId"] != DBNull.Value ? Guid.Parse(r["TenantId"].ToString()!) : Guid.Empty,
        AccountName       = r["AccountName"]?.ToString() ?? "",
        Bsb               = r["Bsb"]?.ToString(),
        AccountNumber     = r["AccountNumber"]?.ToString(),
        Currency          = r["Currency"]?.ToString() ?? "AUD",
        FromDate          = Convert.ToDateTime(r["FromDate"]),
        ToDate            = Convert.ToDateTime(r["ToDate"]),
        OpeningBalance    = Convert.ToDecimal(r["OpeningBalance"]),
        ClosingBalance    = Convert.ToDecimal(r["ClosingBalance"]),
        FileName          = r["FileName"]?.ToString(),
        FilePath          = r["FilePath"]?.ToString(),
        Source            = r["Source"]?.ToString() ?? "manual-csv",
        TransactionCount  = Convert.ToInt32(r["TransactionCount"]),
        ReconciledCount   = Convert.ToInt32(r["ReconciledCount"]),
        UploadedAt        = Convert.ToDateTime(r["UploadedAt"]),
        UploadedBy        = r["UploadedBy"]?.ToString(),
    };

    private static MyDesk.Shared.Models.BankTransaction MapTransaction(DataRow r) => new()
    {
        BankTransactionId = Convert.ToInt32(r["BankTransactionId"]),
        BankStatementId   = Convert.ToInt32(r["BankStatementId"]),
        TenantId          = r.Table.Columns.Contains("TenantId") && r["TenantId"] != DBNull.Value ? Guid.Parse(r["TenantId"].ToString()!) : Guid.Empty,
        TransactionDate   = Convert.ToDateTime(r["TransactionDate"]),
        Description       = r["Description"]?.ToString() ?? "",
        Reference         = r["Reference"]?.ToString(),
        Debit             = Convert.ToDecimal(r["Debit"]),
        Credit            = Convert.ToDecimal(r["Credit"]),
        Balance           = Convert.ToDecimal(r["Balance"]),
        MatchedEntityType = r["MatchedEntityType"]?.ToString(),
        MatchedEntityId   = r["MatchedEntityId"] == DBNull.Value ? null : Convert.ToInt32(r["MatchedEntityId"]),
        Reconciled        = Convert.ToBoolean(r["Reconciled"]),
        Notes             = r["Notes"]?.ToString(),
        CreatedAt         = Convert.ToDateTime(r["CreatedAt"]),
        ReconciledAt      = r["ReconciledAt"] == DBNull.Value ? null : Convert.ToDateTime(r["ReconciledAt"]),
        ReconciledBy      = r["ReconciledBy"]?.ToString(),
    };
}
