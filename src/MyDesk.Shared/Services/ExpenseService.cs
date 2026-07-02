using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ExpenseService
{
    private readonly DatabaseService _db;
    private readonly EmailService _email;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(DatabaseService db, EmailService email, ILogger<ExpenseService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
            BEGIN
                CREATE TABLE Expenses (
                    ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
                    Date DATETIME NOT NULL DEFAULT GETDATE(),
                    Description NVARCHAR(500) NOT NULL DEFAULT '',
                    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    GST DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SupplierId INT NULL,
                    SupplierName NVARCHAR(200) NULL,
                    Category NVARCHAR(100) NOT NULL DEFAULT 'General',
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    FileName NVARCHAR(255) NULL,
                    FilePath NVARCHAR(MAX) NULL,
                    AIProcessingResult NVARCHAR(MAX) NULL,
                    CreatedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_Expenses_Date ON Expenses(Date DESC);
            END
            ELSE
            BEGIN
                -- Migrate old schema: rename Id to ExpenseId
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Id')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExpenseId')
                BEGIN
                    EXEC sp_rename 'Expenses.Id', 'ExpenseId', 'COLUMN';
                END

                -- Migrate old schema: rename Eid to ExpenseId
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Eid')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExpenseId')
                BEGIN
                    EXEC sp_rename 'Expenses.Eid', 'ExpenseId', 'COLUMN';
                END

                -- Migrate old schema: rename ExpenseDate to Date
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExpenseDate')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Date')
                BEGIN
                    EXEC sp_rename 'Expenses.ExpenseDate', 'Date', 'COLUMN';
                END

                -- Migrate old schema: rename ExpenseAmount to Amount
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExpenseAmount')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Amount')
                BEGIN
                    EXEC sp_rename 'Expenses.ExpenseAmount', 'Amount', 'COLUMN';
                END

                -- Migrate old schema: rename Tax to GST
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Tax')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'GST')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'TaxAmount')
                BEGIN
                    EXEC sp_rename 'Expenses.Tax', 'GST', 'COLUMN';
                END

                -- Add missing base columns (legacy schemas may lack any of these)
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Date')
                    ALTER TABLE Expenses ADD Date DATETIME NOT NULL DEFAULT GETDATE();

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Description')
                    ALTER TABLE Expenses ADD Description NVARCHAR(500) NOT NULL DEFAULT '';

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Amount')
                    ALTER TABLE Expenses ADD Amount DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'TaxAmount')
                    ALTER TABLE Expenses ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'GST')
                    ALTER TABLE Expenses ADD GST DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Total')
                    ALTER TABLE Expenses ADD Total DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SupplierId')
                    ALTER TABLE Expenses ADD SupplierId INT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SupplierName')
                    ALTER TABLE Expenses ADD SupplierName NVARCHAR(200) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Category')
                    ALTER TABLE Expenses ADD Category NVARCHAR(100) NOT NULL DEFAULT 'General';

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Status')
                    ALTER TABLE Expenses ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Pending';

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'FileName')
                    ALTER TABLE Expenses ADD FileName NVARCHAR(255) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'FilePath')
                    ALTER TABLE Expenses ADD FilePath NVARCHAR(MAX) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'AIProcessingResult')
                    ALTER TABLE Expenses ADD AIProcessingResult NVARCHAR(MAX) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'CreatedBy')
                    ALTER TABLE Expenses ADD CreatedBy NVARCHAR(100) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'CreatedAt')
                    ALTER TABLE Expenses ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
            END

            ";
        await _db.ExecuteNonQueryAsync(sql);

        // Multi-currency columns. Run as a SEPARATE batch from the DDL above because
        // SQL Server parses the entire batch up-front: if we put the backfill UPDATE in
        // the same batch as the ALTER TABLE ADD, the parser rejects the UPDATE because
        // AmountAud/Currency don't exist yet at parse time (even though the IF guard
        // would prevent execution). We therefore split: 1) add columns, 2) backfill.
        var addColumnsSql = @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Currency')
                ALTER TABLE Expenses ADD Currency NVARCHAR(3) NOT NULL DEFAULT 'AUD';

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'HasGst')
                ALTER TABLE Expenses ADD HasGst BIT NOT NULL DEFAULT 1;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExchangeRate')
                ALTER TABLE Expenses ADD ExchangeRate DECIMAL(18,6) NULL;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'AmountAud')
                ALTER TABLE Expenses ADD AmountAud DECIMAL(18,2) NOT NULL DEFAULT 0;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'AmountAudSource')
                ALTER TABLE Expenses ADD AmountAudSource NVARCHAR(20) NOT NULL DEFAULT 'receipt';";
        await _db.ExecuteNonQueryAsync(addColumnsSql);

        // Backfill AUD-equivalent for any pre-existing AUD rows.
        // The IF EXISTS guards make this resilient to legacy installs that may not
        // have the canonical `Amount` column (some early schemas used `ExpenseAmount`
        // or similar). sp_executesql defers parsing until runtime so missing columns
        // don't fail batch-parse.
        try
        {
            await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Amount')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'AmountAud')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Currency')
BEGIN
    EXEC sp_executesql N'UPDATE Expenses SET AmountAud = Amount WHERE AmountAud = 0 AND Currency = ''AUD''';
END");
        }
        catch (Exception ex)
        {
            // Backfill is a nice-to-have; never fail startup over it. Newly-inserted
            // rows will get the correct AmountAud via the service Normalise() step.
            _logger.LogWarning(ex, "Expense AUD-equivalent backfill skipped (non-fatal).");
        }

        // ── Expense Claims tables ────────────────────────────────────────────
        await _db.ExecuteNonQueryAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseClaims')
            BEGIN
                CREATE TABLE ExpenseClaims (
                    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    ClaimRef NVARCHAR(20) NOT NULL DEFAULT '',
                    ClaimPeriod NVARCHAR(50) NOT NULL DEFAULT '',
                    SubmittedBy NVARCHAR(100) NOT NULL DEFAULT '',
                    SubmittedByUserId INT NULL,
                    ApproverId INT NULL,
                    ApproverName NVARCHAR(100) NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
                    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    TotalGst DECIMAL(18,2) NOT NULL DEFAULT 0,
                    RejectionReason NVARCHAR(1000) NULL,
                    Notes NVARCHAR(2000) NULL,
                    SubmittedAt DATETIME NULL,
                    ApprovedAt DATETIME NULL,
                    FinalisedAt DATETIME NULL,
                    FinalisedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_ExpenseClaims_Status ON ExpenseClaims(Status);
            END");

        await _db.ExecuteNonQueryAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseClaimItems')
            BEGIN
                CREATE TABLE ExpenseClaimItems (
                    ItemId INT IDENTITY(1,1) PRIMARY KEY,
                    ClaimId INT NOT NULL,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    Date DATE NOT NULL DEFAULT GETDATE(),
                    Category NVARCHAR(100) NOT NULL DEFAULT 'General',
                    Supplier NVARCHAR(200) NULL,
                    Description NVARCHAR(500) NOT NULL DEFAULT '',
                    AmountExGst DECIMAL(18,2) NOT NULL DEFAULT 0,
                    GstAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    HasGst BIT NOT NULL DEFAULT 1,
                    ReceiptFileName NVARCHAR(255) NULL,
                    ReceiptFilePath NVARCHAR(500) NULL,
                    Notes NVARCHAR(500) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_ExpenseClaimItems_ClaimId ON ExpenseClaimItems(ClaimId);
            END");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  EXPENSE CLAIMS
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<ExpenseClaim>> GetClaimsAsync(string? status = null, string? submittedBy = null)
    {
        var sql = "SELECT * FROM ExpenseClaims WHERE 1=1";
        var p = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(status)) { sql += " AND Status = @Status"; p["Status"] = status; }
        if (!string.IsNullOrWhiteSpace(submittedBy)) { sql += " AND SubmittedBy = @SubmittedBy"; p["SubmittedBy"] = submittedBy; }
        sql += " ORDER BY CreatedAt DESC";
        var dt = await _db.QueryAsync(sql, p);
        return dt.Map(MapClaimRow);
    }

    public async Task<ExpenseClaim?> GetClaimAsync(int claimId)
    {
        var dt = await _db.QueryAsync("SELECT * FROM ExpenseClaims WHERE ClaimId = @Id", new() { ["Id"] = claimId });
        var claim = dt.Map(MapClaimRow).FirstOrDefault();
        if (claim == null) return null;

        var itemsDt = await _db.QueryAsync("SELECT * FROM ExpenseClaimItems WHERE ClaimId = @Id ORDER BY Date, ItemId", new() { ["Id"] = claimId });
        claim.Items = itemsDt.Map(MapClaimItemRow);
        return claim;
    }

    public async Task<int> SaveClaimAsync(ExpenseClaim claim)
    {
        // Recalculate totals from items if we have them
        if (claim.Items.Count > 0)
        {
            claim.TotalAmount = claim.Items.Sum(i => i.TotalAmount);
            claim.TotalGst    = claim.Items.Sum(i => i.GstAmount);
        }

        if (claim.ClaimId == 0)
        {
            if (string.IsNullOrWhiteSpace(claim.ClaimRef))
                claim.ClaimRef = await GenerateClaimRefAsync();

            var sql = @"
                INSERT INTO ExpenseClaims
                    (ClaimRef, ClaimPeriod, SubmittedBy, SubmittedByUserId, ApproverId, ApproverName,
                     Status, TotalAmount, TotalGst, RejectionReason, Notes,
                     SubmittedAt, ApprovedAt, FinalisedAt, FinalisedBy, CreatedAt, UpdatedAt)
                VALUES
                    (@ClaimRef, @ClaimPeriod, @SubmittedBy, @SubmittedByUserId, @ApproverId, @ApproverName,
                     @Status, @TotalAmount, @TotalGst, @RejectionReason, @Notes,
                     @SubmittedAt, @ApprovedAt, @FinalisedAt, @FinalisedBy, @CreatedAt, @UpdatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            claim.ClaimId = await _db.ScalarAsync<int>(sql, ClaimParams(claim));
        }
        else
        {
            var sql = @"
                UPDATE ExpenseClaims SET
                    ClaimPeriod = @ClaimPeriod, SubmittedBy = @SubmittedBy,
                    SubmittedByUserId = @SubmittedByUserId,
                    ApproverId = @ApproverId, ApproverName = @ApproverName,
                    Status = @Status, TotalAmount = @TotalAmount, TotalGst = @TotalGst,
                    RejectionReason = @RejectionReason, Notes = @Notes,
                    SubmittedAt = @SubmittedAt, ApprovedAt = @ApprovedAt,
                    FinalisedAt = @FinalisedAt, FinalisedBy = @FinalisedBy,
                    UpdatedAt = GETDATE()
                WHERE ClaimId = @ClaimId";
            await _db.ExecuteNonQueryAsync(sql, ClaimParams(claim));
        }
        return claim.ClaimId;
    }

    public async Task<int> SaveClaimItemAsync(ExpenseClaimItem item)
    {
        // Auto-calculate GST
        if (item.HasGst && item.GstAmount == 0 && item.AmountExGst > 0)
            item.GstAmount = Math.Round(item.AmountExGst * 0.1m, 2);
        if (!item.HasGst) item.GstAmount = 0;
        item.TotalAmount = item.AmountExGst + item.GstAmount;

        if (item.ItemId == 0)
        {
            var sql = @"
                INSERT INTO ExpenseClaimItems
                    (ClaimId, Date, Category, Supplier, Description,
                     AmountExGst, GstAmount, TotalAmount, HasGst,
                     ReceiptFileName, ReceiptFilePath, Notes, CreatedAt)
                VALUES
                    (@ClaimId, @Date, @Category, @Supplier, @Description,
                     @AmountExGst, @GstAmount, @TotalAmount, @HasGst,
                     @ReceiptFileName, @ReceiptFilePath, @Notes, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            item.ItemId = await _db.ScalarAsync<int>(sql, ClaimItemParams(item));
        }
        else
        {
            var sql = @"
                UPDATE ExpenseClaimItems SET
                    Date = @Date, Category = @Category, Supplier = @Supplier,
                    Description = @Description, AmountExGst = @AmountExGst,
                    GstAmount = @GstAmount, TotalAmount = @TotalAmount,
                    HasGst = @HasGst, ReceiptFileName = @ReceiptFileName,
                    ReceiptFilePath = @ReceiptFilePath, Notes = @Notes
                WHERE ItemId = @ItemId";
            await _db.ExecuteNonQueryAsync(sql, ClaimItemParams(item));
        }

        // Refresh parent claim totals
        await RefreshClaimTotalsAsync(item.ClaimId);
        return item.ItemId;
    }

    public async Task DeleteClaimItemAsync(int itemId)
    {
        var dt = await _db.QueryAsync("SELECT ClaimId FROM ExpenseClaimItems WHERE ItemId = @Id", new() { ["Id"] = itemId });
        await _db.ExecuteNonQueryAsync("DELETE FROM ExpenseClaimItems WHERE ItemId = @Id", new() { ["Id"] = itemId });
        if (dt.Rows.Count > 0)
        {
            var claimId = Convert.ToInt32(dt.Rows[0]["ClaimId"]);
            await RefreshClaimTotalsAsync(claimId);
        }
    }

    private async Task RefreshClaimTotalsAsync(int claimId)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE ExpenseClaims SET
                TotalAmount = ISNULL((SELECT SUM(TotalAmount) FROM ExpenseClaimItems WHERE ClaimId = @Id), 0),
                TotalGst    = ISNULL((SELECT SUM(GstAmount)   FROM ExpenseClaimItems WHERE ClaimId = @Id), 0),
                UpdatedAt   = GETDATE()
            WHERE ClaimId = @Id",
            new() { ["Id"] = claimId });
    }

    public async Task SubmitClaimAsync(int claimId, int approverId, string approverName, string? approverEmail = null)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE ExpenseClaims SET
                Status = 'Submitted', ApproverId = @ApproverId, ApproverName = @ApproverName,
                SubmittedAt = GETDATE(), UpdatedAt = GETDATE()
            WHERE ClaimId = @ClaimId",
            new() { ["ClaimId"] = claimId, ["ApproverId"] = approverId, ["ApproverName"] = approverName });

        if (!string.IsNullOrWhiteSpace(approverEmail))
        {
            try
            {
                var claim = await GetClaimAsync(claimId);
                if (claim != null)
                {
                    await _email.SendAsync(
                        approverEmail,
                        $"Expense Claim {claim.ClaimRef} Awaiting Your Approval",
                        $"<p>Hi {approverName},</p>" +
                        $"<p>{claim.SubmittedBy} has submitted expense claim <strong>{claim.ClaimRef}</strong> " +
                        $"for period <strong>{claim.ClaimPeriod}</strong> totalling <strong>${claim.TotalAmount:N2}</strong> for your approval.</p>" +
                        $"<p>Please log in to MyDesk to review and approve or reject this claim.</p>");
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send submission email for claim {Id}", claimId); }
        }
    }

    public async Task ApproveClaimAsync(int claimId, string approverName, string? submitterEmail = null)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE ExpenseClaims SET
                Status = 'Approved', ApprovedAt = GETDATE(), UpdatedAt = GETDATE()
            WHERE ClaimId = @ClaimId",
            new() { ["ClaimId"] = claimId });

        if (!string.IsNullOrWhiteSpace(submitterEmail))
        {
            try
            {
                var claim = await GetClaimAsync(claimId);
                if (claim != null)
                {
                    await _email.SendAsync(
                        submitterEmail,
                        $"Expense Claim {claim.ClaimRef} Approved",
                        $"<p>Hi {claim.SubmittedBy},</p>" +
                        $"<p>Your expense claim <strong>{claim.ClaimRef}</strong> " +
                        $"({claim.ClaimPeriod}) totalling <strong>${claim.TotalAmount:N2}</strong> " +
                        $"has been <strong>approved</strong> by {approverName}.</p>" +
                        $"<p>It will now be processed by the Accounts team.</p>");
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send approval email for claim {Id}", claimId); }
        }
    }

    public async Task RejectClaimAsync(int claimId, string reason, string approverName, string? submitterEmail = null)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE ExpenseClaims SET
                Status = 'Rejected', RejectionReason = @Reason, UpdatedAt = GETDATE()
            WHERE ClaimId = @ClaimId",
            new() { ["ClaimId"] = claimId, ["Reason"] = reason });

        if (!string.IsNullOrWhiteSpace(submitterEmail))
        {
            try
            {
                var claim = await GetClaimAsync(claimId);
                if (claim != null)
                {
                    await _email.SendAsync(
                        submitterEmail,
                        $"Expense Claim {claim.ClaimRef} Rejected",
                        $"<p>Hi {claim.SubmittedBy},</p>" +
                        $"<p>Your expense claim <strong>{claim.ClaimRef}</strong> " +
                        $"({claim.ClaimPeriod}) has been <strong>rejected</strong> by {approverName}.</p>" +
                        $"<p><strong>Reason:</strong> {reason}</p>" +
                        $"<p>You may edit and resubmit the claim from MyDesk.</p>");
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send rejection email for claim {Id}", claimId); }
        }
    }

    public async Task FinaliseClaimAsync(int claimId, string finalisedBy)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE ExpenseClaims SET
                Status = 'Finalised', FinalisedAt = GETDATE(), FinalisedBy = @FinalisedBy, UpdatedAt = GETDATE()
            WHERE ClaimId = @ClaimId",
            new() { ["ClaimId"] = claimId, ["FinalisedBy"] = finalisedBy });
    }

    public async Task<string> GenerateClaimRefAsync()
    {
        var year = DateTime.Today.Year;
        var count = await _db.ScalarAsync<int>(
            "SELECT COUNT(*) FROM ExpenseClaims WHERE YEAR(CreatedAt) = @Year",
            new() { ["Year"] = year });
        return $"EXP-{year}-{(count + 1):D3}";
    }

    private static ExpenseClaim MapClaimRow(DataRow r) => new()
    {
        ClaimId           = Convert.ToInt32(r["ClaimId"]),
        ClaimRef          = r["ClaimRef"]?.ToString() ?? "",
        ClaimPeriod       = r["ClaimPeriod"]?.ToString() ?? "",
        SubmittedBy       = r["SubmittedBy"]?.ToString() ?? "",
        SubmittedByUserId = r["SubmittedByUserId"] == DBNull.Value ? null : Convert.ToInt32(r["SubmittedByUserId"]),
        ApproverId        = r["ApproverId"] == DBNull.Value ? null : Convert.ToInt32(r["ApproverId"]),
        ApproverName      = r["ApproverName"]?.ToString(),
        Status            = r["Status"]?.ToString() ?? "Draft",
        TotalAmount       = r["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmount"]) : 0,
        TotalGst          = r["TotalGst"] != DBNull.Value ? Convert.ToDecimal(r["TotalGst"]) : 0,
        RejectionReason   = r["RejectionReason"]?.ToString(),
        Notes             = r["Notes"]?.ToString(),
        SubmittedAt       = r["SubmittedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["SubmittedAt"]),
        ApprovedAt        = r["ApprovedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["ApprovedAt"]),
        FinalisedAt       = r["FinalisedAt"] == DBNull.Value ? null : Convert.ToDateTime(r["FinalisedAt"]),
        FinalisedBy       = r["FinalisedBy"]?.ToString(),
        CreatedAt         = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
        UpdatedAt         = r["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(r["UpdatedAt"]) : DateTime.Now,
    };

    private static ExpenseClaimItem MapClaimItemRow(DataRow r) => new()
    {
        ItemId          = Convert.ToInt32(r["ItemId"]),
        ClaimId         = Convert.ToInt32(r["ClaimId"]),
        Date            = r["Date"] != DBNull.Value ? Convert.ToDateTime(r["Date"]) : DateTime.Today,
        Category        = r["Category"]?.ToString() ?? "General",
        Supplier        = r["Supplier"]?.ToString(),
        Description     = r["Description"]?.ToString() ?? "",
        AmountExGst     = r["AmountExGst"] != DBNull.Value ? Convert.ToDecimal(r["AmountExGst"]) : 0,
        GstAmount       = r["GstAmount"] != DBNull.Value ? Convert.ToDecimal(r["GstAmount"]) : 0,
        TotalAmount     = r["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmount"]) : 0,
        HasGst          = r["HasGst"] != DBNull.Value && Convert.ToBoolean(r["HasGst"]),
        ReceiptFileName = r["ReceiptFileName"]?.ToString(),
        ReceiptFilePath = r["ReceiptFilePath"]?.ToString(),
        Notes           = r["Notes"]?.ToString(),
        CreatedAt       = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
    };

    private static Dictionary<string, object?> ClaimParams(ExpenseClaim c) => new()
    {
        ["ClaimId"]           = c.ClaimId,
        ["ClaimRef"]          = c.ClaimRef,
        ["ClaimPeriod"]       = c.ClaimPeriod,
        ["SubmittedBy"]       = c.SubmittedBy,
        ["SubmittedByUserId"] = (object?)c.SubmittedByUserId ?? DBNull.Value,
        ["ApproverId"]        = (object?)c.ApproverId ?? DBNull.Value,
        ["ApproverName"]      = (object?)c.ApproverName ?? DBNull.Value,
        ["Status"]            = c.Status,
        ["TotalAmount"]       = c.TotalAmount,
        ["TotalGst"]          = c.TotalGst,
        ["RejectionReason"]   = (object?)c.RejectionReason ?? DBNull.Value,
        ["Notes"]             = (object?)c.Notes ?? DBNull.Value,
        ["SubmittedAt"]       = (object?)c.SubmittedAt ?? DBNull.Value,
        ["ApprovedAt"]        = (object?)c.ApprovedAt ?? DBNull.Value,
        ["FinalisedAt"]       = (object?)c.FinalisedAt ?? DBNull.Value,
        ["FinalisedBy"]       = (object?)c.FinalisedBy ?? DBNull.Value,
        ["CreatedAt"]         = c.CreatedAt,
        ["UpdatedAt"]         = c.UpdatedAt,
    };

    private static Dictionary<string, object?> ClaimItemParams(ExpenseClaimItem i) => new()
    {
        ["ItemId"]          = i.ItemId,
        ["ClaimId"]         = i.ClaimId,
        ["Date"]            = i.Date,
        ["Category"]        = i.Category,
        ["Supplier"]        = (object?)i.Supplier ?? DBNull.Value,
        ["Description"]     = i.Description,
        ["AmountExGst"]     = i.AmountExGst,
        ["GstAmount"]       = i.GstAmount,
        ["TotalAmount"]     = i.TotalAmount,
        ["HasGst"]          = i.HasGst,
        ["ReceiptFileName"] = (object?)i.ReceiptFileName ?? DBNull.Value,
        ["ReceiptFilePath"] = (object?)i.ReceiptFilePath ?? DBNull.Value,
        ["Notes"]           = (object?)i.Notes ?? DBNull.Value,
        ["CreatedAt"]       = i.CreatedAt,
    };

    public async Task<List<Expense>> GetExpensesAsync(string? searchTerm = null)
    {
        var sql = "SELECT * FROM Expenses WHERE 1=1";
        
        var parameters = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (Description LIKE @search OR SupplierName LIKE @search OR Category LIKE @search)";
            parameters["search"] = $"%{searchTerm}%";
        }
        sql += " ORDER BY Date DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapRow);
    }

    public async Task<Expense?> GetExpenseAsync(int id)
    {
        var sql = "SELECT * FROM Expenses WHERE ExpenseId = @Id";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = id });
        return dt.Map(MapRow).FirstOrDefault();
    }

    private static Expense MapRow(DataRow r)
    {
        var amount = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0m;
        Func<string, bool> hasColumn = r.Table.Columns.Contains;

        return new Expense
        {
            ExpenseId          = Convert.ToInt32(r["ExpenseId"]),
            Date               = Convert.ToDateTime(r["Date"]),
            Description        = r["Description"]?.ToString() ?? "",
            Amount             = amount,
            TaxAmount          = r["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(r["TaxAmount"]) : 0,
            Gst                = r["GST"]       != DBNull.Value ? Convert.ToDecimal(r["GST"])       : 0,
            Total              = r["Total"]     != DBNull.Value ? Convert.ToDecimal(r["Total"])     : 0,
            SupplierId         = r["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(r["SupplierId"]),
            SupplierName       = r["SupplierName"]?.ToString(),
            Category           = r["Category"]?.ToString() ?? "General",
            Status             = r["Status"]?.ToString() ?? "Pending",
            FileName           = r["FileName"]?.ToString(),
            FilePath           = r["FilePath"]?.ToString(),
            AIProcessingResult = hasColumn("AIProcessingResult") ? r["AIProcessingResult"]?.ToString() : null,
            CreatedBy          = r["CreatedBy"]?.ToString(),
            CreatedAt          = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,

            // Multi-currency
            Currency        = hasColumn("Currency")        && r["Currency"]        != DBNull.Value ? r["Currency"]!.ToString()!        : "AUD",
            HasGst          = hasColumn("HasGst")          && r["HasGst"]          != DBNull.Value ? Convert.ToBoolean(r["HasGst"])     : true,
            ExchangeRate    = hasColumn("ExchangeRate")    && r["ExchangeRate"]    != DBNull.Value ? Convert.ToDecimal(r["ExchangeRate"]) : (decimal?)null,
            AmountAud       = hasColumn("AmountAud")       && r["AmountAud"]       != DBNull.Value ? Convert.ToDecimal(r["AmountAud"])   : amount,
            AmountAudSource = hasColumn("AmountAudSource") && r["AmountAudSource"] != DBNull.Value ? r["AmountAudSource"]!.ToString()!   : "receipt",
        };
    }

    public async Task<int> SaveExpenseAsync(Expense expense)
    {
        Normalise(expense);

        if (expense.ExpenseId == 0)
        {
            var sql = @"
                INSERT INTO Expenses (
                    Date, Description, Amount, TaxAmount, GST, Total,
                    SupplierId, SupplierName, Category, Status,
                    FileName, FilePath, AIProcessingResult,
                    Currency, HasGst, ExchangeRate, AmountAud, AmountAudSource,
                    CreatedBy, CreatedAt)
                VALUES (
                    @Date, @Description, @Amount, @TaxAmount, @GST, @Total,
                    @SupplierId, @SupplierName, @Category, @Status,
                    @FileName, @FilePath, @AIProcessingResult,
                    @Currency, @HasGst, @ExchangeRate, @AmountAud, @AmountAudSource,
                    @CreatedBy, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await _db.ScalarAsync<int>(sql, MapParameters(expense));
        }
        else
        {
            var sql = @"
                UPDATE Expenses SET
                    Date = @Date, Description = @Description, Amount = @Amount, TaxAmount = @TaxAmount,
                    GST = @GST, Total = @Total, SupplierId = @SupplierId, SupplierName = @SupplierName,
                    Category = @Category, Status = @Status, FileName = @FileName, FilePath = @FilePath,
                    AIProcessingResult = @AIProcessingResult,
                    Currency = @Currency, HasGst = @HasGst, ExchangeRate = @ExchangeRate,
                    AmountAud = @AmountAud, AmountAudSource = @AmountAudSource
                WHERE ExpenseId = @ExpenseId";
            await _db.ExecuteNonQueryAsync(sql, MapParameters(expense));
            return expense.ExpenseId;
        }
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await _db.ExecuteNonQueryAsync("DELETE FROM Expenses WHERE ExpenseId = @Id", new() { ["Id"] = id });
    }

    /// <summary>
    /// Finds existing expenses that look like duplicates of the supplied one.
    /// Heuristics (any single match counts, ranked by score):
    ///   * Same supplier (case-insensitive, trimmed) +3
    ///   * Date within ±<paramref name="dayWindow"/> days +2
    ///   * Same amount in the same currency (or AUD-equivalent within $0.01) +3
    ///   * Same description (case-insensitive starts-with first 20 chars) +1
    /// Rows scoring 6 or more are returned.
    /// Tenant scoping is automatic via the SQL Row-Level Security policy on Expenses.
    /// </summary>
    public async Task<List<Expense>> FindPotentialDuplicatesAsync(Expense candidate, int dayWindow = 3)
    {
        // Pre-filter at SQL level for performance: pull a window around the candidate's
        // date with the same currency. Final scoring is done in memory for clarity.
        var dt = await _db.QueryAsync(@"
SELECT TOP 50 *
FROM Expenses
WHERE ABS(DATEDIFF(DAY, Date, @D)) <= @W
  AND ISNULL(Currency, 'AUD') = @Currency
  AND ExpenseId <> @SelfId",
            new()
            {
                ["D"] = candidate.Date.Date,
                ["W"] = dayWindow,
                ["Currency"] = string.IsNullOrWhiteSpace(candidate.Currency) ? "AUD" : candidate.Currency.ToUpperInvariant(),
                ["SelfId"] = candidate.ExpenseId,
            });

        var rows = dt.Map(MapRow);
        var supplier = (candidate.SupplierName ?? "").Trim().ToLowerInvariant();
        var descKey  = (candidate.Description  ?? "").Trim().ToLowerInvariant();
        if (descKey.Length > 20) descKey = descKey[..20];

        var ranked = new List<(Expense expense, int score)>();
        foreach (var r in rows)
        {
            int score = 0;
            if (!string.IsNullOrEmpty(supplier) &&
                string.Equals((r.SupplierName ?? "").Trim(), candidate.SupplierName?.Trim(), StringComparison.OrdinalIgnoreCase))
                score += 3;

            if (Math.Abs((r.Date.Date - candidate.Date.Date).TotalDays) <= dayWindow)
                score += 2;

            // Amount match: primary currency exact OR AUD equivalent within 1 cent.
            if (r.Amount == candidate.Amount && r.Amount > 0)              score += 3;
            else if (Math.Abs(r.AmountAud - candidate.AmountAud) < 0.01m
                     && candidate.AmountAud > 0)                            score += 3;

            var rDescKey = (r.Description ?? "").Trim().ToLowerInvariant();
            if (rDescKey.Length > 20) rDescKey = rDescKey[..20];
            if (descKey.Length > 0 && rDescKey == descKey) score += 1;

            if (score >= 6) ranked.Add((r, score));
        }

        return ranked.OrderByDescending(x => x.score).Select(x => x.expense).ToList();
    }

    /// <summary>
    /// Normalise computed fields before write:
    ///   * Uppercase the currency code.
    ///   * For AUD expenses force AmountAud == Amount and clear ExchangeRate.
    ///   * Compute GST if HasGst is true and the caller didn't supply it (assumes 10%).
    ///   * Default Total to Amount when not supplied.
    /// </summary>
    private static void Normalise(Expense e)
    {
        e.Currency = string.IsNullOrWhiteSpace(e.Currency) ? "AUD" : e.Currency.ToUpperInvariant();

        if (e.Currency == "AUD")
        {
            e.AmountAud = e.Amount;
            e.ExchangeRate = null;
            e.AmountAudSource = "receipt";
        }
        else if (e.AmountAud <= 0)
        {
            // Caller didn't provide AUD equivalent — best we can do: leave 0 so it's
            // visibly missing in reports, but never throw.
            e.AmountAud = 0;
            if (string.IsNullOrWhiteSpace(e.AmountAudSource)) e.AmountAudSource = "manual";
        }

        if (e.HasGst && e.Gst == 0 && e.Currency == "AUD")
        {
            // 10% GST inclusive: GST component = Amount / 11.
            e.Gst = Math.Round(e.Amount / 11m, 2);
            e.TaxAmount = e.Gst;
        }
        if (!e.HasGst)
        {
            e.Gst = 0; e.TaxAmount = 0;
        }

        if (e.Total == 0) e.Total = e.Amount;
    }

    private Dictionary<string, object?> MapParameters(Expense e) => new()
    {
        ["ExpenseId"]          = e.ExpenseId,
        ["Date"]               = e.Date,
        ["Description"]        = e.Description,
        ["Amount"]             = e.Amount,
        ["TaxAmount"]          = e.TaxAmount,
        ["GST"]                = e.Gst,
        ["Total"]              = e.Total,
        ["SupplierId"]         = (object?)e.SupplierId ?? DBNull.Value,
        ["SupplierName"]       = (object?)e.SupplierName ?? DBNull.Value,
        ["Category"]           = e.Category,
        ["Status"]             = e.Status,
        ["FileName"]           = (object?)e.FileName ?? DBNull.Value,
        ["FilePath"]           = (object?)e.FilePath ?? DBNull.Value,
        ["AIProcessingResult"] = (object?)e.AIProcessingResult ?? DBNull.Value,
        ["Currency"]           = e.Currency,
        ["HasGst"]             = e.HasGst,
        ["ExchangeRate"]       = (object?)e.ExchangeRate ?? DBNull.Value,
        ["AmountAud"]          = e.AmountAud,
        ["AmountAudSource"]    = e.AmountAudSource,
        ["CreatedBy"]          = (object?)e.CreatedBy ?? DBNull.Value,
        ["CreatedAt"]          = e.CreatedAt
    };
}
