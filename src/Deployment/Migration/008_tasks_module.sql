-- Migration 008: Create Tasks and TaskComments tables
-- Supports task management with status tracking

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Tasks')
BEGIN
    CREATE TABLE Tasks (
        TaskId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        [Description] NVARCHAR(MAX),
        AssignedToId INT,
        ProjectId INT,
        ProjectName NVARCHAR(200),
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'ToDo',  -- ToDo, InProgress, Done, Cancelled
        Priority NVARCHAR(50) DEFAULT 'Normal',  -- Low, Normal, High, Urgent
        DueDate DATE,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedById INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CompletedAt DATETIME2,
        CONSTRAINT FK_Tasks_Assignee FOREIGN KEY (AssignedToId) REFERENCES Users(UserId),
        CONSTRAINT FK_Tasks_Creator FOREIGN KEY (CreatedById) REFERENCES Users(UserId),
        CONSTRAINT FK_Tasks_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Tasks_TenantId ON Tasks(TenantId);
    CREATE INDEX IX_Tasks_AssignedToId ON Tasks(AssignedToId);
    CREATE INDEX IX_Tasks_Status ON Tasks([Status]);
    CREATE INDEX IX_Tasks_DueDate ON Tasks(DueDate);

    PRINT 'Created Tasks table with indexes';
END
ELSE
    PRINT 'Tasks table already exists';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'TaskComments')
BEGIN
    CREATE TABLE TaskComments (
        CommentId INT PRIMARY KEY IDENTITY(1,1),
        TaskId INT NOT NULL,
        CommentText NVARCHAR(MAX) NOT NULL,
        AuthorId INT NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_TaskComments_Tasks FOREIGN KEY (TaskId) REFERENCES Tasks(TaskId) ON DELETE CASCADE,
        CONSTRAINT FK_TaskComments_Author FOREIGN KEY (AuthorId) REFERENCES Users(UserId),
        CONSTRAINT FK_TaskComments_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_TaskComments_TaskId ON TaskComments(TaskId);
    CREATE INDEX IX_TaskComments_CreatedAt ON TaskComments(CreatedAt);

    PRINT 'Created TaskComments table with indexes';
END
ELSE
    PRINT 'TaskComments table already exists';
