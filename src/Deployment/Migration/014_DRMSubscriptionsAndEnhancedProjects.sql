-- Migration 014: DRM Subscriptions, Enhanced Projects, Timesheets, Charges
-- Date: 2026-05-02
-- Purpose: Replace DRM.xlsx subscription management, add project/timesheet/charge tracking,
--          and expense reimbursement workflow based on the Digital Response DRM spreadsheet

-- ============================================================================
-- Subscriptions - Recurring revenue items (from DRM sheet)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subscriptions')
BEGIN
    CREATE TABLE Subscriptions (
        SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
        ClientName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Category NVARCHAR(100) NOT NULL DEFAULT 'Hosting', -- Hosting, Domain Name Reg, Software, O365, Other
        Schedule NVARCHAR(50) NOT NULL DEFAULT 'Monthly', -- Monthly, Yearly, Every 2 years, Quarterly, One-off
        AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        StartDate DATE NOT NULL,
        NextInvoiceDate DATE NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Cancelled, Suspended, Paused
        Notes NVARCHAR(1000) NULL,
        ApproxCost DECIMAL(18,2) NULL,
        LoginDetails NVARCHAR(500) NULL,
        InvoiceLink NVARCHAR(500) NULL,
        CreatedBy INT NULL,
        CreatedByName NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Subscriptions_ClientName ON Subscriptions(ClientName);
    CREATE INDEX IX_Subscriptions_Status ON Subscriptions(Status);
    CREATE INDEX IX_Subscriptions_NextInvoiceDate ON Subscriptions(NextInvoiceDate);
    CREATE INDEX IX_Subscriptions_Category ON Subscriptions(Category);
    PRINT 'Subscriptions table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Subscriptions') AND name = 'ApproxCost')
        ALTER TABLE Subscriptions ADD ApproxCost DECIMAL(18,2) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Subscriptions') AND name = 'LoginDetails')
        ALTER TABLE Subscriptions ADD LoginDetails NVARCHAR(500) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Subscriptions') AND name = 'InvoiceLink')
        ALTER TABLE Subscriptions ADD InvoiceLink NVARCHAR(500) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Subscriptions') AND name = 'Category')
        ALTER TABLE Subscriptions ADD Category NVARCHAR(100) NOT NULL DEFAULT 'Hosting';
    PRINT 'Subscriptions table verified';
END

-- ============================================================================
-- SubscriptionInvoices - Track each invoiced instance of a subscription
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionInvoices')
BEGIN
    CREATE TABLE SubscriptionInvoices (
        SubInvoiceId INT IDENTITY(1,1) PRIMARY KEY,
        SubscriptionId INT NOT NULL,
        InvoiceNumber NVARCHAR(100) NULL,
        InvoiceDate DATE NOT NULL,
        PeriodStart DATE NULL,
        PeriodEnd DATE NULL,
        AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        GSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        PaidVia NVARCHAR(50) NULL, -- AMP, HSBC, ING, Credit Card, etc.
        IsClaimed BIT NOT NULL DEFAULT 0,
        ClaimedInExpenseReport NVARCHAR(50) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_SubscriptionInvoices_Subscriptions FOREIGN KEY (SubscriptionId) REFERENCES Subscriptions(SubscriptionId) ON DELETE CASCADE
    );
    CREATE INDEX IX_SubscriptionInvoices_SubscriptionId ON SubscriptionInvoices(SubscriptionId);
    CREATE INDEX IX_SubscriptionInvoices_InvoiceDate ON SubscriptionInvoices(InvoiceDate DESC);
    CREATE INDEX IX_SubscriptionInvoices_IsClaimed ON SubscriptionInvoices(IsClaimed);
    PRINT 'SubscriptionInvoices table created';
END

-- ============================================================================
-- Enhanced Projects - Extended project tracking from DRM.xlsx Projects sheet
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DRMProjects')
BEGIN
    CREATE TABLE DRMProjects (
        ProjectId INT IDENTITY(1,1) PRIMARY KEY,
        QuoteRef NVARCHAR(50) NULL,
        ClientName NVARCHAR(200) NOT NULL,
        ProjectName NVARCHAR(300) NOT NULL,
        ProjectType NVARCHAR(50) NOT NULL DEFAULT 'Fixed', -- Fixed, Time & Materials, Subscription
        QuoteAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        HourlyRateDefault DECIMAL(18,2) NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Completed, On Hold, Cancelled
        PONumber NVARCHAR(100) NULL,
        BudgetHours DECIMAL(8,2) NULL,
        ActualHours DECIMAL(8,2) NOT NULL DEFAULT 0,
        UnbilledHours DECIMAL(8,2) NOT NULL DEFAULT 0,
        AlreadyInvoiced DECIMAL(18,2) NOT NULL DEFAULT 0,
        BilledAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnbilledAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CostAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Profit DECIMAL(18,2) NOT NULL DEFAULT 0,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_DRMProjects_ClientName ON DRMProjects(ClientName);
    CREATE INDEX IX_DRMProjects_Status ON DRMProjects(Status);
    CREATE INDEX IX_DRMProjects_QuoteRef ON DRMProjects(QuoteRef);
    PRINT 'DRMProjects table created';
END
ELSE
BEGIN
    -- Add any missing columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'QuoteRef')
        ALTER TABLE DRMProjects ADD QuoteRef NVARCHAR(50) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'ProjectType')
        ALTER TABLE DRMProjects ADD ProjectType NVARCHAR(50) NOT NULL DEFAULT 'Fixed';
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'HourlyRateDefault')
        ALTER TABLE DRMProjects ADD HourlyRateDefault DECIMAL(18,2) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'BudgetHours')
        ALTER TABLE DRMProjects ADD BudgetHours DECIMAL(8,2) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'ActualHours')
        ALTER TABLE DRMProjects ADD ActualHours DECIMAL(8,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'UnbilledHours')
        ALTER TABLE DRMProjects ADD UnbilledHours DECIMAL(8,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'AlreadyInvoiced')
        ALTER TABLE DRMProjects ADD AlreadyInvoiced DECIMAL(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'BilledAmount')
        ALTER TABLE DRMProjects ADD BilledAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'UnbilledAmount')
        ALTER TABLE DRMProjects ADD UnbilledAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'CostAmount')
        ALTER TABLE DRMProjects ADD CostAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'Profit')
        ALTER TABLE DRMProjects ADD Profit DECIMAL(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DRMProjects') AND name = 'UpdatedAt')
        ALTER TABLE DRMProjects ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
    PRINT 'DRMProjects table verified';
END

-- ============================================================================
-- DRM_TimesheetEntries - Time tracking linked to projects
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DRM_TimesheetEntries')
BEGIN
    CREATE TABLE DRM_TimesheetEntries (
        EntryId INT IDENTITY(1,1) PRIMARY KEY,
        EntryDate DATE NOT NULL,
        ConsultantId INT NOT NULL,
        ConsultantName NVARCHAR(100) NOT NULL,
        ClientName NVARCHAR(200) NOT NULL,
        ProjectName NVARCHAR(300) NOT NULL,
        Task NVARCHAR(200) NULL,
        Description NVARCHAR(1000) NOT NULL,
        Hours DECIMAL(5,2) NOT NULL DEFAULT 0,
        IsBillable BIT NOT NULL DEFAULT 1,
        IsInvoiced BIT NOT NULL DEFAULT 0,
        HourlyRate DECIMAL(18,2) NOT NULL DEFAULT 0,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CostRate DECIMAL(18,2) NOT NULL DEFAULT 0,
        CostAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        DRMProjectId INT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DRM_TimesheetEntries_DRMProjects FOREIGN KEY (DRMProjectId) REFERENCES DRMProjects(ProjectId) ON DELETE SET NULL
    );
    CREATE INDEX IX_DRM_TimesheetEntries_EntryDate ON DRM_TimesheetEntries(EntryDate DESC);
    CREATE INDEX IX_DRM_TimesheetEntries_ConsultantId ON DRM_TimesheetEntries(ConsultantId);
    CREATE INDEX IX_DRM_TimesheetEntries_ClientName ON DRM_TimesheetEntries(ClientName);
    CREATE INDEX IX_DRM_TimesheetEntries_ProjectName ON DRM_TimesheetEntries(ProjectName);
    CREATE INDEX IX_DRM_TimesheetEntries_IsBillable ON DRM_TimesheetEntries(IsBillable);
    CREATE INDEX IX_DRM_TimesheetEntries_IsInvoiced ON DRM_TimesheetEntries(IsInvoiced);
    PRINT 'DRM_TimesheetEntries table created';
END

-- ============================================================================
-- DRMCharges - Non-time charges linked to projects
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DRMCharges')
BEGIN
    CREATE TABLE DRMCharges (
        ChargeId INT IDENTITY(1,1) PRIMARY KEY,
        ChargeDate DATE NOT NULL,
        ClientName NVARCHAR(200) NOT NULL,
        ProjectName NVARCHAR(300) NOT NULL,
        Category NVARCHAR(100) NOT NULL DEFAULT 'General', -- Hosting, Software, Travel, etc.
        Description NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsInvoiced BIT NOT NULL DEFAULT 0,
        Cost DECIMAL(18,2) NOT NULL DEFAULT 0,
        Notes NVARCHAR(500) NULL,
        DRMProjectId INT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DRMCharges_DRMProjects FOREIGN KEY (DRMProjectId) REFERENCES DRMProjects(ProjectId) ON DELETE SET NULL
    );
    CREATE INDEX IX_DRMCharges_ChargeDate ON DRMCharges(ChargeDate DESC);
    CREATE INDEX IX_DRMCharges_ClientName ON DRMCharges(ClientName);
    CREATE INDEX IX_DRMCharges_IsInvoiced ON DRMCharges(IsInvoiced);
    PRINT 'DRMCharges table created';
END

-- ============================================================================
-- ExpenseReports - Monthly expense report headers (replaces Excel tabs)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseReports')
BEGIN
    CREATE TABLE ExpenseReports (
        ReportId INT IDENTITY(1,1) PRIMARY KEY,
        ReportPeriod DATE NOT NULL, -- First day of the month
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Submitted, Approved, Reimbursed, Rejected
        SubmittedBy INT NULL,
        SubmittedByName NVARCHAR(100) NULL,
        SubmittedDate DATETIME NULL,
        ApprovedBy INT NULL,
        ApprovedByName NVARCHAR(100) NULL,
        ApprovedDate DATETIME NULL,
        ReimbursedDate DATETIME NULL,
        ReimbursementAmount DECIMAL(18,2) NULL,
        ReimbursementNotes NVARCHAR(500) NULL,
        TotalExGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalInclGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        OwnerType NVARCHAR(10) NOT NULL DEFAULT 'DR', -- DR or PB
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_ExpenseReports_ReportPeriod ON ExpenseReports(ReportPeriod DESC);
    CREATE INDEX IX_ExpenseReports_Status ON ExpenseReports(Status);
    CREATE INDEX IX_ExpenseReports_SubmittedBy ON ExpenseReports(SubmittedBy);
    PRINT 'ExpenseReports table created';
END

-- ============================================================================
-- ExpenseReportLines - Individual expense items within a report
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseReportLines')
BEGIN
    CREATE TABLE ExpenseReportLines (
        LineId INT IDENTITY(1,1) PRIMARY KEY,
        ReportId INT NOT NULL,
        ExpenseDate DATE NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Category NVARCHAR(100) NOT NULL DEFAULT 'General',
        AmountExGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        GSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        AmountInclGST DECIMAL(18,2) NOT NULL DEFAULT 0,
        OwnerType NVARCHAR(10) NOT NULL DEFAULT 'DR', -- DR (company) or PB (personal/tax return)
        Classification NVARCHAR(100) NULL,
        HasReceipt BIT NOT NULL DEFAULT 0,
        ReceiptFileName NVARCHAR(255) NULL,
        ReceiptFilePath NVARCHAR(500) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ExpenseReportLines_Reports FOREIGN KEY (ReportId) REFERENCES ExpenseReports(ReportId) ON DELETE CASCADE
    );
    CREATE INDEX IX_ExpenseReportLines_ReportId ON ExpenseReportLines(ReportId);
    CREATE INDEX IX_ExpenseReportLines_Category ON ExpenseReportLines(Category);
    CREATE INDEX IX_ExpenseReportLines_OwnerType ON ExpenseReportLines(OwnerType);
    PRINT 'ExpenseReportLines table created';
END

-- ============================================================================
-- O365Subscriptions - Office 365 service tracking
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'O365Subscriptions')
BEGIN
    CREATE TABLE O365Subscriptions (
        O365SubId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceName NVARCHAR(200) NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        UserName NVARCHAR(100) NULL,
        BillingCycle NVARCHAR(50) NOT NULL DEFAULT 'Monthly', -- Monthly, Yearly, ---
        DateCommenced DATE NULL,
        CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        SellPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_O365Subscriptions_CustomerName ON O365Subscriptions(CustomerName);
    CREATE INDEX IX_O365Subscriptions_IsActive ON O365Subscriptions(IsActive);
    PRINT 'O365Subscriptions table created';
END

-- ============================================================================
-- Passwords - Secure password/credential storage (from DRM Passwords sheet)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemCredentials')
BEGIN
    CREATE TABLE SystemCredentials (
        CredentialId INT IDENTITY(1,1) PRIMARY KEY,
        SiteName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        Website NVARCHAR(500) NULL,
        Username NVARCHAR(200) NULL,
        EncryptedPassword NVARCHAR(500) NULL,
        Category NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_SystemCredentials_SiteName ON SystemCredentials(SiteName);
    CREATE INDEX IX_SystemCredentials_Category ON SystemCredentials(Category);
    PRINT 'SystemCredentials table created';
END

PRINT 'Migration 014 complete - DRM, Projects, Timesheets, Charges, Expense Reports, O365, Credentials';
