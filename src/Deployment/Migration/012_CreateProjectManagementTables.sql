-- Migration 012: Create ProjectManagement tables
-- Date: 2026-05-02
-- Purpose: Project management with agile scrum board, tasks, and capacity planning

-- Projects table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE Projects (
        ProjectId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        OwnerId INT NOT NULL,
        OwnerName NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, On Hold, Completed, Cancelled
        Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium', -- Low, Medium, High, Critical
        StartDate DATE NULL,
        EndDate DATE NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Projects_Status ON Projects(Status);
    CREATE INDEX IX_Projects_OwnerId ON Projects(OwnerId);
    PRINT 'Projects table created';
END

-- Sprints table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sprints')
BEGIN
    CREATE TABLE Sprints (
        SprintId INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Planning', -- Planning, Active, Completed
        Goal NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Sprints_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE
    );
    CREATE INDEX IX_Sprints_ProjectId ON Sprints(ProjectId);
    PRINT 'Sprints table created';
END

-- Tasks table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE Tasks (
        TaskId INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NULL,
        SprintId INT NULL,
        Title NVARCHAR(300) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        AssigneeId INT NULL,
        AssigneeName NVARCHAR(100) NULL,
        ReporterId INT NULL,
        ReporterName NVARCHAR(100) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Backlog', -- Backlog, To Do, In Progress, In Review, Done
        Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium', -- Low, Medium, High, Critical
        StoryPoints INT NULL,
        TaskType NVARCHAR(50) NOT NULL DEFAULT 'Task', -- Task, Bug, Feature, Epic, Story
        Tags NVARCHAR(500) NULL,
        DueDate DATE NULL,
        EstimatedHours DECIMAL(5,2) NULL,
        ActualHours DECIMAL(5,2) NULL,
        ParentTaskId INT NULL,
        OrderIndex INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CompletedAt DATETIME NULL,
        CONSTRAINT FK_Tasks_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE SET NULL,
        CONSTRAINT FK_Tasks_Sprints FOREIGN KEY (SprintId) REFERENCES Sprints(SprintId) ON DELETE NO ACTION,
        CONSTRAINT FK_Tasks_ParentTasks FOREIGN KEY (ParentTaskId) REFERENCES Tasks(TaskId)
    );
    CREATE INDEX IX_Tasks_ProjectId ON Tasks(ProjectId);
    CREATE INDEX IX_Tasks_SprintId ON Tasks(SprintId);
    CREATE INDEX IX_Tasks_Status ON Tasks(Status);
    CREATE INDEX IX_Tasks_AssigneeId ON Tasks(AssigneeId);
    PRINT 'Tasks table created';
END

-- Capacity Planning table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CapacityPlanning')
BEGIN
    CREATE TABLE CapacityPlanning (
        CapacityId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        WeekStartDate DATE NOT NULL,
        AvailableHours DECIMAL(4,2) NOT NULL DEFAULT 40,
        AllocatedHours DECIMAL(4,2) NOT NULL DEFAULT 0,
        LeaveHours DECIMAL(4,2) NOT NULL DEFAULT 0,
        Notes NVARCHAR(500) NULL,
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE UNIQUE INDEX IX_CapacityPlanning_UserWeek ON CapacityPlanning(UserId, WeekStartDate);
    CREATE INDEX IX_CapacityPlanning_WeekStartDate ON CapacityPlanning(WeekStartDate DESC);
    PRINT 'CapacityPlanning table created';
END

-- Task Comments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskComments')
BEGIN
    CREATE TABLE TaskComments (
        CommentId INT IDENTITY(1,1) PRIMARY KEY,
        TaskId INT NOT NULL,
        UserId INT NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        Comment NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_TaskComments_Tasks FOREIGN KEY (TaskId) REFERENCES Tasks(TaskId) ON DELETE CASCADE
    );
    CREATE INDEX IX_TaskComments_TaskId ON TaskComments(TaskId);
    PRINT 'TaskComments table created';
END
