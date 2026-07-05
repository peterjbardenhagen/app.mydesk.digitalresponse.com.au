-- Migration 007: Create Timesheets and TimesheetEntries tables
-- Supports time tracking with project allocation

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Timesheets')
BEGIN
    CREATE TABLE Timesheets (
        TimesheetId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        EmployeeId INT NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WeekStartDate DATE NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',  -- Draft, Submitted, Approved, Rejected
        TotalHours DECIMAL(7,2) NOT NULL DEFAULT 0,
        SubmittedDate DATETIME2,
        ApprovedDate DATETIME2,
        ApprovedBy INT,
        Notes NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_Timesheets_Users FOREIGN KEY (EmployeeId) REFERENCES Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_Timesheets_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT FK_Timesheets_Approver FOREIGN KEY (ApprovedBy) REFERENCES Users(UserId)
    );

    CREATE INDEX IX_Timesheets_TenantId ON Timesheets(TenantId);
    CREATE INDEX IX_Timesheets_EmployeeId ON Timesheets(EmployeeId);
    CREATE INDEX IX_Timesheets_Status ON Timesheets([Status]);

    PRINT 'Created Timesheets table with indexes';
END
ELSE
    PRINT 'Timesheets table already exists';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'TimesheetEntries')
BEGIN
    CREATE TABLE TimesheetEntries (
        EntryId INT PRIMARY KEY IDENTITY(1,1),
        TimesheetId INT NOT NULL,
        [Date] DATE NOT NULL,
        ProjectId INT,
        ProjectName NVARCHAR(200),
        [Description] NVARCHAR(500),
        Hours DECIMAL(7,2) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_TimesheetEntries_Timesheets FOREIGN KEY (TimesheetId) REFERENCES Timesheets(TimesheetId) ON DELETE CASCADE,
        CONSTRAINT FK_TimesheetEntries_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_TimesheetEntries_TimesheetId ON TimesheetEntries(TimesheetId);
    CREATE INDEX IX_TimesheetEntries_Date ON TimesheetEntries([Date]);

    PRINT 'Created TimesheetEntries table with indexes';
END
ELSE
    PRINT 'TimesheetEntries table already exists';
