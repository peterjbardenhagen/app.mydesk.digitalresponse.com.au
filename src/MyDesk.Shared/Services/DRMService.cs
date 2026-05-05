using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class DRMService
{
    private readonly DatabaseService _db;

    public DRMService(DatabaseService db)
    {
        _db = db;
    }

    public async Task EnsureTablesAsync()
    {
        await _db.ExecuteNonQueryAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subscriptions')
            BEGIN
                CREATE TABLE Subscriptions (SubscriptionId INT IDENTITY(1,1) PRIMARY KEY, ClientName NVARCHAR(200) NOT NULL, Description NVARCHAR(500) NOT NULL, Category NVARCHAR(100) NOT NULL DEFAULT 'Hosting', Schedule NVARCHAR(50) NOT NULL DEFAULT 'Monthly', AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0, AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0, StartDate DATE NOT NULL, NextInvoiceDate DATE NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Active', Notes NVARCHAR(1000) NULL, ApproxCost DECIMAL(18,2) NULL, LoginDetails NVARCHAR(500) NULL, InvoiceLink NVARCHAR(500) NULL, CreatedBy INT NULL, CreatedByName NVARCHAR(100) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionInvoices')
            BEGIN
                CREATE TABLE SubscriptionInvoices (SubInvoiceId INT IDENTITY(1,1) PRIMARY KEY, SubscriptionId INT NOT NULL, InvoiceNumber NVARCHAR(100) NULL, InvoiceDate DATE NOT NULL, PeriodStart DATE NULL, PeriodEnd DATE NULL, AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0, AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0, GSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0, PaidVia NVARCHAR(50) NULL, IsClaimed BIT NOT NULL DEFAULT 0, ClaimedInExpenseReport NVARCHAR(50) NULL, Notes NVARCHAR(500) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DRMCharges')
            BEGIN
                CREATE TABLE DRMCharges (ChargeId INT IDENTITY(1,1) PRIMARY KEY, ChargeDate DATE NOT NULL, ClientName NVARCHAR(200) NOT NULL, ProjectName NVARCHAR(300) NOT NULL, Category NVARCHAR(100) NOT NULL DEFAULT 'General', Description NVARCHAR(500) NOT NULL, Amount DECIMAL(18,2) NOT NULL DEFAULT 0, IsInvoiced BIT NOT NULL DEFAULT 0, Cost DECIMAL(18,2) NOT NULL DEFAULT 0, Notes NVARCHAR(500) NULL, DRMProjectId INT NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseReports')
            BEGIN
                CREATE TABLE ExpenseReports (ReportId INT IDENTITY(1,1) PRIMARY KEY, ReportPeriod DATE NOT NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', SubmittedBy INT NULL, SubmittedByName NVARCHAR(100) NULL, SubmittedDate DATETIME NULL, ApprovedBy INT NULL, ApprovedByName NVARCHAR(100) NULL, ApprovedDate DATETIME NULL, ReimbursedDate DATETIME NULL, ReimbursementAmount DECIMAL(18,2) NULL, ReimbursementNotes NVARCHAR(500) NULL, TotalExGST DECIMAL(18,2) NOT NULL DEFAULT 0, TotalGST DECIMAL(18,2) NOT NULL DEFAULT 0, TotalInclGST DECIMAL(18,2) NOT NULL DEFAULT 0, OwnerType NVARCHAR(10) NOT NULL DEFAULT 'DR', CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseReportLines')
            BEGIN
                CREATE TABLE ExpenseReportLines (LineId INT IDENTITY(1,1) PRIMARY KEY, ReportId INT NOT NULL, ExpenseDate DATE NOT NULL, Description NVARCHAR(500) NOT NULL, Category NVARCHAR(100) NOT NULL DEFAULT 'General', AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0, GSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0, AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0, OwnerType NVARCHAR(10) NOT NULL DEFAULT 'DR', Classification NVARCHAR(100) NULL, HasReceipt BIT NOT NULL DEFAULT 0, ReceiptFileName NVARCHAR(255) NULL, ReceiptFilePath NVARCHAR(500) NULL, Notes NVARCHAR(500) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), CONSTRAINT FK_ExpenseReportLines_Reports FOREIGN KEY (ReportId) REFERENCES ExpenseReports(ReportId) ON DELETE CASCADE);
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'O365Subscriptions')
            BEGIN
                CREATE TABLE O365Subscriptions (O365SubId INT IDENTITY(1,1) PRIMARY KEY, ServiceName NVARCHAR(200) NOT NULL, CustomerName NVARCHAR(200) NOT NULL, UserName NVARCHAR(100) NULL, BillingCycle NVARCHAR(50) NOT NULL DEFAULT 'Monthly', DateCommenced DATE NULL, CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0, SellPrice DECIMAL(18,2) NOT NULL DEFAULT 0, IsActive BIT NOT NULL DEFAULT 1, Notes NVARCHAR(500) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemCredentials')
            BEGIN
                CREATE TABLE SystemCredentials (CredentialId INT IDENTITY(1,1) PRIMARY KEY, SiteName NVARCHAR(200) NOT NULL, Description NVARCHAR(500) NULL, Website NVARCHAR(500) NULL, Username NVARCHAR(200) NULL, EncryptedPassword NVARCHAR(500) NULL, Category NVARCHAR(100) NULL, IsActive BIT NOT NULL DEFAULT 1, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END");
    }

    // ==================== SUBSCRIPTIONS ====================
    public async Task<List<DRMSubscription>> GetSubscriptionsAsync(string? status = null, string? category = null)
    {
        var sql = "SELECT * FROM Subscriptions WHERE 1=1";
        if (!string.IsNullOrEmpty(status)) sql += " AND Status = @Status";
        if (!string.IsNullOrEmpty(category)) sql += " AND Category = @Category";
        sql += " ORDER BY ClientName, Category";
        return (await _db.QueryAsync<DRMSubscription>(sql, new { Status = status, Category = category })).ToList();
    }

    public async Task<DRMSubscription?> GetSubscriptionAsync(int id)
    {
        return await _db.QueryFirstOrDefaultAsync<DRMSubscription>("SELECT * FROM Subscriptions WHERE SubscriptionId = @Id", new { Id = id });
    }

    public async Task<int> CreateSubscriptionAsync(DRMSubscription sub)
    {
        const string sql = @"INSERT INTO Subscriptions (ClientName, Description, Category, Schedule, AmountInclGST, AmountExGST, StartDate, NextInvoiceDate, Status, Notes, ApproxCost, LoginDetails, InvoiceLink, CreatedBy, CreatedByName, CreatedAt, UpdatedAt)
            VALUES (@ClientName, @Description, @Category, @Schedule, @AmountInclGST, @AmountExGST, @StartDate, @NextInvoiceDate, @Status, @Notes, @ApproxCost, @LoginDetails, @InvoiceLink, @CreatedBy, @CreatedByName, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, sub);
    }

    public async Task UpdateSubscriptionAsync(DRMSubscription sub)
    {
        await _db.ExecuteObjAsync("UPDATE Subscriptions SET ClientName = @ClientName, Description = @Description, Category = @Category, Schedule = @Schedule, AmountInclGST = @AmountInclGST, AmountExGST = @AmountExGST, StartDate = @StartDate, NextInvoiceDate = @NextInvoiceDate, Status = @Status, Notes = @Notes, ApproxCost = @ApproxCost, LoginDetails = @LoginDetails, InvoiceLink = @InvoiceLink, UpdatedAt = GETDATE() WHERE SubscriptionId = @SubscriptionId", sub);
    }

    public async Task DeleteSubscriptionAsync(int id)
    {
        await _db.ExecuteObjAsync("DELETE FROM Subscriptions WHERE SubscriptionId = @Id", new { Id = id });
    }

    public async Task<List<DRMSubscription>> GetSubscriptionsDueForInvoiceAsync(DateTime asOfDate)
    {
        return (await _db.QueryAsync<DRMSubscription>(
            "SELECT * FROM Subscriptions WHERE Status = 'Active' AND (NextInvoiceDate IS NULL OR NextInvoiceDate <= @AsOfDate) ORDER BY NextInvoiceDate",
            new { AsOfDate = asOfDate.Date })).ToList();
    }

    /// <summary>
    /// Subscriptions whose next renewal falls within the given window (default 30 days).
    /// Used by the dashboard "upcoming renewals" reminder.
    /// </summary>
    public async Task<List<DRMSubscription>> GetUpcomingRenewalsAsync(int daysAhead = 30)
    {
        var until = DateTime.Today.AddDays(daysAhead);
        return (await _db.QueryAsync<DRMSubscription>(
            "SELECT * FROM Subscriptions WHERE Status = 'Active' AND NextInvoiceDate IS NOT NULL AND NextInvoiceDate <= @Until ORDER BY NextInvoiceDate",
            new { Until = until })).ToList();
    }

    /// <summary>
    /// Returns the next invoice date based on the current next date and the schedule cadence.
    /// </summary>
    public static DateTime AdvanceSchedule(DateTime current, string schedule)
    {
        return schedule?.ToLower() switch
        {
            "weekly"      => current.AddDays(7),
            "fortnightly" => current.AddDays(14),
            "monthly"     => current.AddMonths(1),
            "quarterly"   => current.AddMonths(3),
            "half-yearly" => current.AddMonths(6),
            "halfyearly"  => current.AddMonths(6),
            "biannual"    => current.AddMonths(6),
            "yearly"      => current.AddYears(1),
            "annual"      => current.AddYears(1),
            "biennial"    => current.AddYears(2),
            "bi-annual"   => current.AddYears(2),
            _              => current.AddMonths(1),
        };
    }

    /// <summary>
    /// Raise a real Invoice from a Subscription, log the SubscriptionInvoice, and
    /// roll the subscription's NextInvoiceDate forward according to its schedule.
    /// </summary>
    public async Task<int> RaiseInvoiceFromSubscriptionAsync(int subscriptionId, string userCode, InvoiceService invoiceSvc, CompanyService companySvc)
    {
        var sub = await GetSubscriptionAsync(subscriptionId)
                  ?? throw new InvalidOperationException($"Subscription {subscriptionId} not found.");

        // Try to match the client to an existing customer company. If none, create a stub.
        var companies = await companySvc.GetCompaniesAsync();
        var company = companies.FirstOrDefault(c =>
            string.Equals(c.CompanyName?.Trim(), sub.ClientName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (company == null)
        {
            company = new Company { CompanyName = sub.ClientName, IsCustomer = true };
            company.CompanyId = await companySvc.SaveCompanyAsync(company);
        }

        var periodStart = sub.NextInvoiceDate ?? DateTime.Today;
        var periodEnd   = AdvanceSchedule(periodStart, sub.Schedule).AddDays(-1);
        var description = $"{sub.Description} ({periodStart:dd/MM/yyyy} – {periodEnd:dd/MM/yyyy})";

        var invoice = new Invoice
        {
            InvoiceDate     = DateTime.Today,
            Code            = $"SUB-{sub.SubscriptionId}",
            CompanyId       = company.CompanyId,
            InvCompany      = sub.ClientName,
            CustomerNotes   = $"Subscription renewal: {sub.Category} – {sub.Schedule}",
            InternalNotes   = sub.Notes,
            NettPriceTotal  = sub.AmountExGST,
            GSTTotal        = sub.AmountInclGST - sub.AmountExGST,
        };
        var lines = new List<InvoiceLineItem>
        {
            new InvoiceLineItem
            {
                ProductCode  = sub.Category,
                Description  = description,
                Quantity     = 1,
                NettPrice    = sub.AmountExGST,
                ExtNettPrice = sub.AmountExGST,
            }
        };

        var invoiceId = await invoiceSvc.CreateInvoiceAsync(invoice, lines, userCode);

        await CreateSubscriptionInvoiceAsync(new SubscriptionInvoice
        {
            SubscriptionId = sub.SubscriptionId,
            InvoiceNumber  = invoiceId.ToString(),
            InvoiceDate    = DateTime.Today,
            PeriodStart    = periodStart,
            PeriodEnd      = periodEnd,
            AmountExGST    = sub.AmountExGST,
            AmountInclGST  = sub.AmountInclGST,
            GSTAmount      = sub.AmountInclGST - sub.AmountExGST,
            Notes          = $"Auto-generated from subscription {sub.SubscriptionId}",
        });

        // Advance the subscription's next invoice date.
        var newNext = AdvanceSchedule(periodStart, sub.Schedule);
        await _db.ExecuteObjAsync(
            "UPDATE Subscriptions SET NextInvoiceDate = @Next, UpdatedAt = GETDATE() WHERE SubscriptionId = @Id",
            new { Next = newNext, Id = sub.SubscriptionId });

        return invoiceId;
    }

    // ==================== SUBSCRIPTION INVOICES ====================
    public async Task<List<SubscriptionInvoice>> GetSubscriptionInvoicesAsync(int subscriptionId)
    {
        return (await _db.QueryAsync<SubscriptionInvoice>(
            "SELECT * FROM SubscriptionInvoices WHERE SubscriptionId = @SubId ORDER BY InvoiceDate DESC",
            new { SubId = subscriptionId })).ToList();
    }

    public async Task<int> CreateSubscriptionInvoiceAsync(SubscriptionInvoice inv)
    {
        const string sql = @"INSERT INTO SubscriptionInvoices (SubscriptionId, InvoiceNumber, InvoiceDate, PeriodStart, PeriodEnd, AmountInclGST, AmountExGST, GSTAmount, PaidVia, IsClaimed, ClaimedInExpenseReport, Notes, CreatedAt)
            VALUES (@SubscriptionId, @InvoiceNumber, @InvoiceDate, @PeriodStart, @PeriodEnd, @AmountInclGST, @AmountExGST, @GSTAmount, @PaidVia, @IsClaimed, @ClaimedInExpenseReport, @Notes, GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, inv);
    }

    public async Task UpdateSubscriptionInvoiceAsync(SubscriptionInvoice inv)
    {
        await _db.ExecuteObjAsync("UPDATE SubscriptionInvoices SET InvoiceNumber = @InvoiceNumber, PaidVia = @PaidVia, IsClaimed = @IsClaimed, ClaimedInExpenseReport = @ClaimedInExpenseReport, Notes = @Notes WHERE SubInvoiceId = @SubInvoiceId", inv);
    }

    // ==================== CHARGES ====================
    public async Task<List<DRMCharge>> GetChargesAsync(DateTime? startDate = null, DateTime? endDate = null, string? client = null, bool? isInvoiced = null)
    {
        var sql = "SELECT * FROM DRMCharges WHERE 1=1";
        if (startDate.HasValue) sql += " AND ChargeDate >= @StartDate";
        if (endDate.HasValue) sql += " AND ChargeDate <= @EndDate";
        if (!string.IsNullOrEmpty(client)) sql += " AND ClientName LIKE @Client";
        if (isInvoiced.HasValue) sql += " AND IsInvoiced = @IsInvoiced";
        sql += " ORDER BY ChargeDate DESC";
        return (await _db.QueryAsync<DRMCharge>(sql, new { StartDate = startDate, EndDate = endDate, Client = $"%{client}%", IsInvoiced = isInvoiced })).ToList();
    }

    public async Task<int> CreateChargeAsync(DRMCharge charge)
    {
        const string sql = @"INSERT INTO DRMCharges (ChargeDate, ClientName, ProjectName, Category, Description, Amount, IsInvoiced, Cost, Notes, DRMProjectId, CreatedAt, UpdatedAt)
            VALUES (@ChargeDate, @ClientName, @ProjectName, @Category, @Description, @Amount, @IsInvoiced, @Cost, @Notes, @DRMProjectId, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, charge);
    }

    public async Task UpdateChargeAsync(DRMCharge charge)
    {
        await _db.ExecuteObjAsync("UPDATE DRMCharges SET ChargeDate = @ChargeDate, Category = @Category, Description = @Description, Amount = @Amount, IsInvoiced = @IsInvoiced, Cost = @Cost, Notes = @Notes, UpdatedAt = GETDATE() WHERE ChargeId = @ChargeId", charge);
    }

    public async Task MarkChargeInvoicedAsync(int id)
    {
        await _db.ExecuteObjAsync("UPDATE DRMCharges SET IsInvoiced = 1, UpdatedAt = GETDATE() WHERE ChargeId = @Id", new { Id = id });
    }

    // ==================== EXPENSE REPORTS ====================
    public async Task<List<ExpenseReport>> GetExpenseReportsAsync(string? status = null, string? ownerType = null)
    {
        var sql = "SELECT * FROM ExpenseReports WHERE 1=1";
        if (!string.IsNullOrEmpty(status)) sql += " AND Status = @Status";
        if (!string.IsNullOrEmpty(ownerType)) sql += " AND OwnerType = @OwnerType";
        sql += " ORDER BY ReportPeriod DESC";
        return (await _db.QueryAsync<ExpenseReport>(sql, new { Status = status, OwnerType = ownerType })).ToList();
    }

    public async Task<ExpenseReport?> GetExpenseReportAsync(int id)
    {
        return await _db.QueryFirstOrDefaultAsync<ExpenseReport>("SELECT * FROM ExpenseReports WHERE ReportId = @Id", new { Id = id });
    }

    public async Task<ExpenseReport> GetOrCreateExpenseReportAsync(DateTime period, string ownerType, int userId, string userName)
    {
        var existing = await _db.QueryFirstOrDefaultAsync<ExpenseReport>(
            "SELECT * FROM ExpenseReports WHERE ReportPeriod = @Period AND OwnerType = @OwnerType",
            new { Period = period.Date, OwnerType = ownerType });
        
        if (existing != null) return existing;

        const string sql = @"INSERT INTO ExpenseReports (ReportPeriod, Status, OwnerType, CreatedAt, UpdatedAt)
            VALUES (@Period, 'Draft', @OwnerType, GETDATE(), GETDATE()); SELECT * FROM ExpenseReports WHERE ReportId = CAST(SCOPE_IDENTITY() AS INT);";
        return (await _db.QueryAsync<ExpenseReport>(sql, new { Period = period.Date, OwnerType = ownerType })).First();
    }

    public async Task UpdateExpenseReportAsync(ExpenseReport report)
    {
        await _db.ExecuteObjAsync("UPDATE ExpenseReports SET Status = @Status, SubmittedBy = @SubmittedBy, SubmittedByName = @SubmittedByName, SubmittedDate = @SubmittedDate, ApprovedBy = @ApprovedBy, ApprovedByName = @ApprovedByName, ApprovedDate = @ApprovedDate, ReimbursedDate = @ReimbursedDate, ReimbursementAmount = @ReimbursementAmount, ReimbursementNotes = @ReimbursementNotes, TotalExGST = @TotalExGST, TotalGST = @TotalGST, TotalInclGST = @TotalInclGST, UpdatedAt = GETDATE() WHERE ReportId = @ReportId", report);
    }

    public async Task SubmitExpenseReportAsync(int reportId, int userId, string userName)
    {
        await _db.ExecuteObjAsync(
            "UPDATE ExpenseReports SET Status = 'Submitted', SubmittedBy = @UserId, SubmittedByName = @UserName, SubmittedDate = GETDATE(), UpdatedAt = GETDATE() WHERE ReportId = @Id",
            new { Id = reportId, UserId = userId, UserName = userName });
    }

    public async Task ApproveExpenseReportAsync(int reportId, int approverId, string approverName, decimal reimbursementAmount)
    {
        await _db.ExecuteObjAsync(
            "UPDATE ExpenseReports SET Status = 'Approved', ApprovedBy = @ApproverId, ApprovedByName = @ApproverName, ApprovedDate = GETDATE(), ReimbursementAmount = @Amount, UpdatedAt = GETDATE() WHERE ReportId = @Id",
            new { Id = reportId, ApproverId = approverId, ApproverName = approverName, Amount = reimbursementAmount });
    }

    public async Task ReimburseExpenseReportAsync(int reportId)
    {
        await _db.ExecuteObjAsync(
            "UPDATE ExpenseReports SET Status = 'Reimbursed', ReimbursedDate = GETDATE(), UpdatedAt = GETDATE() WHERE ReportId = @Id",
            new { Id = reportId });
    }

    // ==================== EXPENSE REPORT LINES ====================
    public async Task<List<ExpenseReportLine>> GetExpenseReportLinesAsync(int reportId)
    {
        return (await _db.QueryAsync<ExpenseReportLine>(
            "SELECT * FROM ExpenseReportLines WHERE ReportId = @ReportId ORDER BY ExpenseDate",
            new { ReportId = reportId })).ToList();
    }

    public async Task<int> AddExpenseReportLineAsync(ExpenseReportLine line)
    {
        const string sql = @"INSERT INTO ExpenseReportLines (ReportId, ExpenseDate, Description, Category, AmountExGST, GSTAmount, AmountInclGST, OwnerType, Classification, HasReceipt, ReceiptFileName, ReceiptFilePath, Notes, CreatedAt)
            VALUES (@ReportId, @ExpenseDate, @Description, @Category, @AmountExGST, @GSTAmount, @AmountInclGST, @OwnerType, @Classification, @HasReceipt, @ReceiptFileName, @ReceiptFilePath, @Notes, GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, line);
    }

    public async Task UpdateExpenseReportLineAsync(ExpenseReportLine line)
    {
        await _db.ExecuteObjAsync("UPDATE ExpenseReportLines SET ExpenseDate = @ExpenseDate, Description = @Description, Category = @Category, AmountExGST = @AmountExGST, GSTAmount = @GSTAmount, AmountInclGST = @AmountInclGST, OwnerType = @OwnerType, Classification = @Classification, HasReceipt = @HasReceipt, ReceiptFileName = @ReceiptFileName, ReceiptFilePath = @ReceiptFilePath, Notes = @Notes WHERE LineId = @LineId", line);
    }

    public async Task DeleteExpenseReportLineAsync(int lineId)
    {
        await _db.ExecuteObjAsync("DELETE FROM ExpenseReportLines WHERE LineId = @Id", new { Id = lineId });
    }

    // ==================== O365 SUBSCRIPTIONS ====================
    public async Task<List<O365Subscription>> GetO365SubscriptionsAsync(string? customer = null)
    {
        var sql = "SELECT * FROM O365Subscriptions WHERE 1=1";
        if (!string.IsNullOrEmpty(customer)) sql += " AND CustomerName LIKE @Customer";
        sql += " ORDER BY CustomerName, ServiceName";
        return (await _db.QueryAsync<O365Subscription>(sql, new { Customer = $"%{customer}%" })).ToList();
    }

    public async Task<int> CreateO365SubscriptionAsync(O365Subscription sub)
    {
        const string sql = @"INSERT INTO O365Subscriptions (ServiceName, CustomerName, UserName, BillingCycle, DateCommenced, CostPrice, SellPrice, IsActive, Notes, CreatedAt, UpdatedAt)
            VALUES (@ServiceName, @CustomerName, @UserName, @BillingCycle, @DateCommenced, @CostPrice, @SellPrice, @IsActive, @Notes, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, sub);
    }

    public async Task UpdateO365SubscriptionAsync(O365Subscription sub)
    {
        await _db.ExecuteObjAsync("UPDATE O365Subscriptions SET ServiceName = @ServiceName, CustomerName = @CustomerName, UserName = @UserName, BillingCycle = @BillingCycle, DateCommenced = @DateCommenced, CostPrice = @CostPrice, SellPrice = @SellPrice, IsActive = @IsActive, Notes = @Notes, UpdatedAt = GETDATE() WHERE O365SubId = @O365SubId", sub);
    }

    // ==================== SYSTEM CREDENTIALS ====================
    public async Task<List<SystemCredential>> GetCredentialsAsync(string? category = null)
    {
        var sql = "SELECT * FROM SystemCredentials WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(category)) sql += " AND Category = @Category";
        sql += " ORDER BY SiteName";
        return (await _db.QueryAsync<SystemCredential>(sql, new { Category = category })).ToList();
    }

    public async Task<int> CreateCredentialAsync(SystemCredential cred)
    {
        const string sql = @"INSERT INTO SystemCredentials (SiteName, Description, Website, Username, EncryptedPassword, Category, IsActive, CreatedAt, UpdatedAt)
            VALUES (@SiteName, @Description, @Website, @Username, @EncryptedPassword, @Category, @IsActive, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, cred);
    }

    public async Task UpdateCredentialAsync(SystemCredential cred)
    {
        await _db.ExecuteObjAsync("UPDATE SystemCredentials SET SiteName = @SiteName, Description = @Description, Website = @Website, Username = @Username, EncryptedPassword = @EncryptedPassword, Category = @Category, IsActive = @IsActive, UpdatedAt = GETDATE() WHERE CredentialId = @CredentialId", cred);
    }
}
