-- Migration 006: Create Expenses and ExpenseItems tables
-- Supports expense tracking with receipts and approval workflow

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Expenses')
BEGIN
    CREATE TABLE Expenses (
        ExpenseId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        EmployeeId INT NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        Description NVARCHAR(500),
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',  -- Draft, Submitted, Approved, Rejected
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CurrencyCode NVARCHAR(3) DEFAULT 'AUD',
        [Date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        SubmittedDate DATETIME2,
        ApprovedDate DATETIME2,
        ApprovedBy INT,
        Notes NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_Expenses_Users FOREIGN KEY (EmployeeId) REFERENCES Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_Expenses_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT FK_Expenses_Approver FOREIGN KEY (ApprovedBy) REFERENCES Users(UserId)
    );

    CREATE INDEX IX_Expenses_TenantId ON Expenses(TenantId);
    CREATE INDEX IX_Expenses_EmployeeId ON Expenses(EmployeeId);
    CREATE INDEX IX_Expenses_Status ON Expenses([Status]);
    CREATE INDEX IX_Expenses_Date ON Expenses([Date]);

    PRINT 'Created Expenses table with indexes';
END
ELSE
    PRINT 'Expenses table already exists';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ExpenseItems')
BEGIN
    CREATE TABLE ExpenseItems (
        ExpenseItemId INT PRIMARY KEY IDENTITY(1,1),
        ExpenseId INT NOT NULL,
        Category NVARCHAR(100) NOT NULL,  -- Travel, Meals, Accommodation, Supplies, Other
        Description NVARCHAR(500),
        Amount DECIMAL(18,2) NOT NULL,
        ReceiptUrl NVARCHAR(500),
        [Date] DATETIME2 NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ExpenseItems_Expenses FOREIGN KEY (ExpenseId) REFERENCES Expenses(ExpenseId) ON DELETE CASCADE,
        CONSTRAINT FK_ExpenseItems_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_ExpenseItems_ExpenseId ON ExpenseItems(ExpenseId);
    CREATE INDEX IX_ExpenseItems_Category ON ExpenseItems(Category);

    PRINT 'Created ExpenseItems table with indexes';
END
ELSE
    PRINT 'ExpenseItems table already exists';
