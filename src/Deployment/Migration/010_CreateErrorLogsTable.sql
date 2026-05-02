-- Migration 010: Create ErrorLogs table for centralized error tracking
-- Date: 2026-05-02
-- Purpose: Store application errors in the database for better debugging and monitoring

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ErrorLogs')
BEGIN
    CREATE TABLE ErrorLogs (
        ErrorLogId INT IDENTITY(1,1) PRIMARY KEY,
        ErrorDate DATETIME NOT NULL DEFAULT GETDATE(),
        Severity NVARCHAR(20) NOT NULL DEFAULT 'Error', -- Error, Warning, Critical, Info
        ExceptionType NVARCHAR(255) NULL,
        Message NVARCHAR(MAX) NOT NULL,
        StackTrace NVARCHAR(MAX) NULL,
        InnerException NVARCHAR(MAX) NULL,
        RequestUrl NVARCHAR(500) NULL,
        HttpMethod NVARCHAR(10) NULL,
        UserAgent NVARCHAR(500) NULL,
        IPAddress NVARCHAR(45) NULL,
        UserId NVARCHAR(100) NULL,
        UserName NVARCHAR(100) NULL,
        CorrelationId NVARCHAR(100) NULL,
        Source NVARCHAR(100) NULL, -- Which component/service threw the error
        IsResolved BIT NOT NULL DEFAULT 0,
        ResolvedBy NVARCHAR(100) NULL,
        ResolvedDate DATETIME NULL,
        ResolutionNotes NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );

    -- Create indexes for common queries
    CREATE INDEX IX_ErrorLogs_ErrorDate ON ErrorLogs(ErrorDate DESC);
    CREATE INDEX IX_ErrorLogs_Severity ON ErrorLogs(Severity);
    CREATE INDEX IX_ErrorLogs_IsResolved ON ErrorLogs(IsResolved);
    CREATE INDEX IX_ErrorLogs_UserId ON ErrorLogs(UserId);
    CREATE INDEX IX_ErrorLogs_Source ON ErrorLogs(Source);

    PRINT 'ErrorLogs table created successfully';
END
ELSE
BEGIN
    -- Ensure all columns exist for existing tables
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'CorrelationId')
        ALTER TABLE ErrorLogs ADD CorrelationId NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'Source')
        ALTER TABLE ErrorLogs ADD Source NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'IsResolved')
        ALTER TABLE ErrorLogs ADD IsResolved BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'ResolvedBy')
        ALTER TABLE ErrorLogs ADD ResolvedBy NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'ResolvedDate')
        ALTER TABLE ErrorLogs ADD ResolvedDate DATETIME NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'ResolutionNotes')
        ALTER TABLE ErrorLogs ADD ResolutionNotes NVARCHAR(1000) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'UserId')
        ALTER TABLE ErrorLogs ADD UserId NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'UserName')
        ALTER TABLE ErrorLogs ADD UserName NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'HttpMethod')
        ALTER TABLE ErrorLogs ADD HttpMethod NVARCHAR(10) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'Severity')
        ALTER TABLE ErrorLogs ADD Severity NVARCHAR(20) NOT NULL DEFAULT 'Error';

    PRINT 'ErrorLogs table columns verified/updated';
END
