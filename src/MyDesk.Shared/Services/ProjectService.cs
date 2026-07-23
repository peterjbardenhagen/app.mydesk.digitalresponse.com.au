using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ProjectService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(DatabaseService db, ILogger<ProjectService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTablesAsync()
    {
        const string sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
            BEGIN
                CREATE TABLE Projects (ProjectId INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Description NVARCHAR(1000) NULL, OwnerId INT NOT NULL, OwnerName NVARCHAR(100) NOT NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Active', Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium', StartDate DATE NULL, EndDate DATE NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sprints')
            BEGIN
                CREATE TABLE Sprints (SprintId INT IDENTITY(1,1) PRIMARY KEY, ProjectId INT NOT NULL, Name NVARCHAR(100) NOT NULL, StartDate DATE NOT NULL, EndDate DATE NOT NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Planning', Goal NVARCHAR(500) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
            BEGIN
                CREATE TABLE Tasks (TaskId INT IDENTITY(1,1) PRIMARY KEY, ProjectId INT NULL, SprintId INT NULL, Title NVARCHAR(300) NOT NULL, Description NVARCHAR(MAX) NULL, AssigneeId INT NULL, AssigneeName NVARCHAR(100) NULL, ReporterId INT NULL, ReporterName NVARCHAR(100) NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Backlog', Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium', StoryPoints INT NULL, TaskType NVARCHAR(50) NOT NULL DEFAULT 'Task', Tags NVARCHAR(500) NULL, DueDate DATE NULL, EstimatedHours DECIMAL(5,2) NULL, ActualHours DECIMAL(5,2) NULL, ParentTaskId INT NULL, OrderIndex INT NOT NULL DEFAULT 0, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(), CompletedAt DATETIME NULL);
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CapacityPlanning')
            BEGIN
                CREATE TABLE CapacityPlanning (CapacityId INT IDENTITY(1,1) PRIMARY KEY, UserId INT NOT NULL, UserName NVARCHAR(100) NOT NULL, WeekStartDate DATE NOT NULL, AvailableHours DECIMAL(4,2) NOT NULL DEFAULT 40, AllocatedHours DECIMAL(4,2) NOT NULL DEFAULT 0, LeaveHours DECIMAL(4,2) NOT NULL DEFAULT 0, Notes NVARCHAR(500) NULL, UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskComments')
            BEGIN
                CREATE TABLE TaskComments (CommentId INT IDENTITY(1,1) PRIMARY KEY, TaskId INT NOT NULL, UserId INT NOT NULL, UserName NVARCHAR(100) NOT NULL, Comment NVARCHAR(1000) NOT NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE());
            END
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectMembers')
                CREATE TABLE ProjectMembers (MemberId INT IDENTITY PRIMARY KEY, ProjectId INT NOT NULL, UserId INT NULL, UserCode NVARCHAR(50) NOT NULL DEFAULT '', Name NVARCHAR(150) NOT NULL, Email NVARCHAR(200) NULL, Role NVARCHAR(50) NOT NULL DEFAULT 'TeamMember', PortalAccess BIT NOT NULL DEFAULT 0, CanApproveChanges BIT NOT NULL DEFAULT 0, AddedAt DATETIME NOT NULL DEFAULT GETDATE());
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectMilestones')
                CREATE TABLE ProjectMilestones (MilestoneId INT IDENTITY PRIMARY KEY, ProjectId INT NOT NULL, Title NVARCHAR(300) NOT NULL, Description NVARCHAR(1000) NULL, DueDate DATE NOT NULL, ActualDate DATE NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', SignOffRequired BIT NOT NULL DEFAULT 0, SignOffDate DATETIME NULL, SignOffBy NVARCHAR(150) NULL, IsUAT BIT NOT NULL DEFAULT 0, OrderIndex INT NOT NULL DEFAULT 0, CreatedAt DATETIME NOT NULL DEFAULT GETDATE());
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectCostEntries')
                CREATE TABLE ProjectCostEntries (CostEntryId INT IDENTITY PRIMARY KEY, ProjectId INT NOT NULL, TaskId INT NULL, EntryType NVARCHAR(50) NOT NULL DEFAULT 'Labour', Description NVARCHAR(500) NOT NULL, Quantity DECIMAL(10,2) NOT NULL DEFAULT 1, UnitRate DECIMAL(12,2) NOT NULL DEFAULT 0, Amount DECIMAL(12,2) NOT NULL DEFAULT 0, UserCode NVARCHAR(50) NULL, EnteredBy NVARCHAR(150) NULL, EntryDate DATE NOT NULL, IsBillable BIT NOT NULL DEFAULT 1, IsApproved BIT NOT NULL DEFAULT 0, CreatedAt DATETIME NOT NULL DEFAULT GETDATE());
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChangeRequests')
                CREATE TABLE ChangeRequests (ChangeRequestId INT IDENTITY PRIMARY KEY, ProjectId INT NOT NULL, ChangeNumber NVARCHAR(20) NOT NULL DEFAULT '', Title NVARCHAR(300) NOT NULL, Description NVARCHAR(MAX) NOT NULL, ImpactDescription NVARCHAR(1000) NULL, ImpactDays INT NULL, ImpactCost DECIMAL(12,2) NULL, Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', RaisedBy NVARCHAR(150) NOT NULL, RaisedByCode NVARCHAR(50) NULL, RaisedAt DATETIME NOT NULL DEFAULT GETDATE(), ApprovedBy NVARCHAR(150) NULL, ApprovedAt DATETIME NULL, RejectionReason NVARCHAR(500) NULL, LinkedQuoteId INT NULL, LinkedInvoiceId INT NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RisksIssues')
                CREATE TABLE RisksIssues (RiskIssueId INT IDENTITY PRIMARY KEY, ProjectId INT NOT NULL, ItemType NVARCHAR(10) NOT NULL DEFAULT 'Risk', Title NVARCHAR(300) NOT NULL, Description NVARCHAR(MAX) NULL, Likelihood NVARCHAR(20) NOT NULL DEFAULT 'Medium', Impact NVARCHAR(20) NOT NULL DEFAULT 'Medium', Priority NVARCHAR(20) NOT NULL DEFAULT 'Medium', Status NVARCHAR(50) NOT NULL DEFAULT 'Open', OwnedBy NVARCHAR(150) NULL, OwnedByCode NVARCHAR(50) NULL, MitigationPlan NVARCHAR(MAX) NULL, AssignedTaskId INT NULL, DueDate DATE NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE(), UpdatedAt DATETIME NOT NULL DEFAULT GETDATE());";
        await _db.ExecuteNonQueryAsync(sql);

        // Migrate: add CreatedAt / UpdatedAt if the Projects table was created without them
        const string migrate = @"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'CreatedAt')
                    ALTER TABLE Projects ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'UpdatedAt')
                    ALTER TABLE Projects ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'OwnerName')
                    ALTER TABLE Projects ADD OwnerName NVARCHAR(100) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'Priority')
                    ALTER TABLE Projects ADD Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'StartDate')
                    ALTER TABLE Projects ADD StartDate DATE NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Projects') AND name = 'EndDate')
                    ALTER TABLE Projects ADD EndDate DATE NULL;
            END";
        await _db.ExecuteNonQueryAsync(migrate);

        // Migrate: new Projects columns
        const string migrateProjects = @"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='CommercialModel')
                ALTER TABLE Projects ADD CommercialModel NVARCHAR(50) NOT NULL DEFAULT 'FixedPrice';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ClientCompanyId')
                ALTER TABLE Projects ADD ClientCompanyId INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ClientCompanyName')
                ALTER TABLE Projects ADD ClientCompanyName NVARCHAR(200) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='LinkedQuoteId')
                ALTER TABLE Projects ADD LinkedQuoteId INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='LinkedInvoiceId')
                ALTER TABLE Projects ADD LinkedInvoiceId INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='BudgetAmount')
                ALTER TABLE Projects ADD BudgetAmount DECIMAL(12,2) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ActualCost')
                ALTER TABLE Projects ADD ActualCost DECIMAL(12,2) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='PercentComplete')
                ALTER TABLE Projects ADD PercentComplete INT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='PredictedEndDate')
                ALTER TABLE Projects ADD PredictedEndDate DATE NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='PredictedFinalCost')
                ALTER TABLE Projects ADD PredictedFinalCost DECIMAL(12,2) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ProjectCode')
                ALTER TABLE Projects ADD ProjectCode NVARCHAR(50) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='ClientProjectManager')
                ALTER TABLE Projects ADD ClientProjectManager NVARCHAR(150) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Projects') AND name='PortalToken')
                ALTER TABLE Projects ADD PortalToken NVARCHAR(100) NULL;";
        await _db.ExecuteNonQueryAsync(migrateProjects);

        // Migrate: new Tasks columns
        const string migrateTasks = @"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Tasks') AND name='PercentComplete')
                ALTER TABLE Tasks ADD PercentComplete INT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Tasks') AND name='TaskStartDate')
                ALTER TABLE Tasks ADD TaskStartDate DATE NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Tasks') AND name='TaskEndDate')
                ALTER TABLE Tasks ADD TaskEndDate DATE NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Tasks') AND name='MilestoneId')
                ALTER TABLE Tasks ADD MilestoneId INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Tasks') AND name='MilestoneName')
                ALTER TABLE Tasks ADD MilestoneName NVARCHAR(300) NULL;";
        await _db.ExecuteNonQueryAsync(migrateTasks);
    }

    // -------------------------------------------------------------------------
    // Projects
    // -------------------------------------------------------------------------

    public async Task<List<Project>> GetProjectsAsync(string? status = null)
    {
        var sql = "SELECT * FROM Projects WHERE 1=1";
        if (!string.IsNullOrEmpty(status)) sql += " AND Status = @Status";
        sql += " ORDER BY CreatedAt DESC";
        return (await _db.QueryAsync<Project>(sql, new { Status = status })).ToList();
    }

    public async Task<Project?> GetProjectAsync(int projectId)
    {
        return await _db.QueryFirstOrDefaultAsync<Project>("SELECT * FROM Projects WHERE ProjectId = @ProjectId", new { ProjectId = projectId });
    }

    public async Task<int> CreateProjectAsync(Project project)
    {
        const string sql = @"INSERT INTO Projects (Name, Description, OwnerId, OwnerName, Status, Priority, StartDate, EndDate, CreatedAt, UpdatedAt)
            VALUES (@Name, @Description, @OwnerId, @OwnerName, @Status, @Priority, @StartDate, @EndDate, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, project);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        await _db.ExecuteObjAsync("UPDATE Projects SET Name = @Name, Description = @Description, Status = @Status, Priority = @Priority, StartDate = @StartDate, EndDate = @EndDate, UpdatedAt = GETDATE() WHERE ProjectId = @ProjectId", project);
    }

    /// <summary>Updates all project columns including the new commercial/budget fields.</summary>
    public async Task UpdateProjectFullAsync(Project project)
    {
        const string sql = @"
            UPDATE Projects SET
                Name = @Name,
                Description = @Description,
                Status = @Status,
                Priority = @Priority,
                StartDate = @StartDate,
                EndDate = @EndDate,
                CommercialModel = @CommercialModel,
                ClientCompanyId = @ClientCompanyId,
                ClientCompanyName = @ClientCompanyName,
                LinkedQuoteId = @LinkedQuoteId,
                LinkedInvoiceId = @LinkedInvoiceId,
                BudgetAmount = @BudgetAmount,
                ProjectCode = @ProjectCode,
                ClientProjectManager = @ClientProjectManager,
                PortalToken = @PortalToken,
                UpdatedAt = GETDATE()
            WHERE ProjectId = @ProjectId";
        await _db.ExecuteObjAsync(sql, project);
    }

    /// <summary>Recomputes PercentComplete (avg of task PercentComplete) and ActualCost (sum of cost entries) for a project.</summary>
    public async Task RefreshProjectStatsAsync(int projectId)
    {
        const string sql = @"
            UPDATE Projects SET
                PercentComplete = ISNULL((SELECT AVG(CAST(PercentComplete AS DECIMAL(5,2))) FROM Tasks WHERE ProjectId = @ProjectId), 0),
                ActualCost      = ISNULL((SELECT SUM(Amount) FROM ProjectCostEntries WHERE ProjectId = @ProjectId), 0),
                UpdatedAt       = GETDATE()
            WHERE ProjectId = @ProjectId";
        await _db.ExecuteNonQueryAsync(sql, new() { ["ProjectId"] = projectId });
    }

    /// <summary>Returns the project's PortalToken, generating and persisting one if it does not yet exist.</summary>
    public async Task<string> EnsurePortalTokenAsync(int projectId)
    {
        var project = await GetProjectAsync(projectId);
        if (project == null) throw new InvalidOperationException($"Project {projectId} not found.");

        if (!string.IsNullOrEmpty(project.PortalToken))
            return project.PortalToken;

        var token = Guid.NewGuid().ToString("N");
        await _db.ExecuteNonQueryAsync(
            "UPDATE Projects SET PortalToken = @Token, UpdatedAt = GETDATE() WHERE ProjectId = @ProjectId",
            new() { ["Token"] = token, ["ProjectId"] = projectId });
        return token;
    }

    // -------------------------------------------------------------------------
    // Sprints
    // -------------------------------------------------------------------------

    public async Task<List<Sprint>> GetSprintsAsync(int projectId, string? status = null)
    {
        var sql = "SELECT * FROM Sprints WHERE ProjectId = @ProjectId";
        if (!string.IsNullOrEmpty(status)) sql += " AND Status = @Status";
        sql += " ORDER BY StartDate DESC";
        return (await _db.QueryAsync<Sprint>(sql, new { ProjectId = projectId, Status = status })).ToList();
    }

    public async Task<int> CreateSprintAsync(Sprint sprint)
    {
        const string sql = @"INSERT INTO Sprints (ProjectId, Name, StartDate, EndDate, Status, Goal, CreatedAt)
            VALUES (@ProjectId, @Name, @StartDate, @EndDate, @Status, @Goal, GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, sprint);
    }

    // -------------------------------------------------------------------------
    // Tasks
    // -------------------------------------------------------------------------

    public async Task<List<TaskItem>> GetTasksAsync(int? projectId = null, int? sprintId = null, string? status = null, int? assigneeId = null)
    {
        var sql = "SELECT * FROM Tasks WHERE 1=1";
        if (projectId.HasValue) sql += " AND ProjectId = @ProjectId";
        if (sprintId.HasValue) sql += " AND SprintId = @SprintId";
        if (!string.IsNullOrEmpty(status)) sql += " AND Status = @Status";
        if (assigneeId.HasValue) sql += " AND AssigneeId = @AssigneeId";
        sql += " ORDER BY OrderIndex, CreatedAt DESC";
        return (await _db.QueryAsync<TaskItem>(sql, new { ProjectId = projectId, SprintId = sprintId, Status = status, AssigneeId = assigneeId })).ToList();
    }

    public async Task<TaskItem?> GetTaskAsync(int taskId)
    {
        return await _db.QueryFirstOrDefaultAsync<TaskItem>("SELECT * FROM Tasks WHERE TaskId = @TaskId", new { TaskId = taskId });
    }

    public async Task<int> CreateTaskAsync(TaskItem task)
    {
        const string sql = @"INSERT INTO Tasks (ProjectId, SprintId, Title, Description, AssigneeId, AssigneeName, ReporterId, ReporterName, Status, Priority, StoryPoints, TaskType, Tags, DueDate, EstimatedHours, ActualHours, ParentTaskId, OrderIndex, CreatedAt, UpdatedAt)
            VALUES (@ProjectId, @SprintId, @Title, @Description, @AssigneeId, @AssigneeName, @ReporterId, @ReporterName, @Status, @Priority, @StoryPoints, @TaskType, @Tags, @DueDate, @EstimatedHours, @ActualHours, @ParentTaskId, @OrderIndex, GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, task);
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        await _db.ExecuteObjAsync("UPDATE Tasks SET Title = @Title, Description = @Description, AssigneeId = @AssigneeId, AssigneeName = @AssigneeName, Status = @Status, Priority = @Priority, StoryPoints = @StoryPoints, TaskType = @TaskType, Tags = @Tags, DueDate = @DueDate, EstimatedHours = @EstimatedHours, ActualHours = @ActualHours, UpdatedAt = GETDATE(), CompletedAt = CASE WHEN @Status = 'Done' THEN GETDATE() ELSE CompletedAt END WHERE TaskId = @TaskId", task);
    }

    public async Task UpdateTaskStatusAsync(int taskId, string status)
    {
        await _db.ExecuteObjAsync("UPDATE Tasks SET Status = @Status, UpdatedAt = GETDATE(), CompletedAt = CASE WHEN @Status = 'Done' THEN GETDATE() ELSE CompletedAt END WHERE TaskId = @TaskId", new { TaskId = taskId, Status = status });
    }

    /// <summary>Updates the PercentComplete for a task and refreshes the parent project's computed stats.</summary>
    public async Task UpdateTaskPercentAsync(int taskId, int percent)
    {
        await _db.ExecuteNonQueryAsync(
            "UPDATE Tasks SET PercentComplete = @Percent, UpdatedAt = GETDATE() WHERE TaskId = @TaskId",
            new() { ["TaskId"] = taskId, ["Percent"] = percent });

        // Refresh project-level aggregates
        var task = await GetTaskAsync(taskId);
        if (task?.ProjectId != null)
            await RefreshProjectStatsAsync(task.ProjectId.Value);
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        await _db.ExecuteObjAsync("DELETE FROM Tasks WHERE TaskId = @TaskId", new { TaskId = taskId });
    }

    // -------------------------------------------------------------------------
    // Capacity Planning
    // -------------------------------------------------------------------------

    public async Task<List<CapacityPlanning>> GetCapacityAsync(DateTime weekStartDate)
    {
        return (await _db.QueryAsync<CapacityPlanning>("SELECT * FROM CapacityPlanning WHERE WeekStartDate = @WeekStartDate ORDER BY UserName", new { WeekStartDate = weekStartDate.Date })).ToList();
    }

    public async Task<CapacityPlanning?> GetCapacityForUserAsync(int userId, DateTime weekStartDate)
    {
        return await _db.QueryFirstOrDefaultAsync<CapacityPlanning>("SELECT * FROM CapacityPlanning WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate", new { UserId = userId, WeekStartDate = weekStartDate.Date });
    }

    public async Task UpdateCapacityAsync(CapacityPlanning capacity)
    {
        const string sql = @"
            IF EXISTS (SELECT 1 FROM CapacityPlanning WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate)
            BEGIN
                UPDATE CapacityPlanning SET AvailableHours = @AvailableHours, AllocatedHours = @AllocatedHours, LeaveHours = @LeaveHours, Notes = @Notes, UpdatedAt = GETDATE() WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate;
            END
            ELSE
            BEGIN
                INSERT INTO CapacityPlanning (UserId, UserName, WeekStartDate, AvailableHours, AllocatedHours, LeaveHours, Notes, UpdatedAt) VALUES (@UserId, @UserName, @WeekStartDate, @AvailableHours, @AllocatedHours, @LeaveHours, @Notes, GETDATE());
            END";
        await _db.ExecuteObjAsync(sql, capacity);
    }

    // -------------------------------------------------------------------------
    // Task Comments
    // -------------------------------------------------------------------------

    public async Task<List<TaskComment>> GetTaskCommentsAsync(int taskId)
    {
        return (await _db.QueryAsync<TaskComment>("SELECT * FROM TaskComments WHERE TaskId = @TaskId ORDER BY CreatedAt ASC", new { TaskId = taskId })).ToList();
    }

    public async Task AddTaskCommentAsync(TaskComment comment)
    {
        await _db.ExecuteObjAsync("INSERT INTO TaskComments (TaskId, UserId, UserName, Comment, CreatedAt) VALUES (@TaskId, @UserId, @UserName, @Comment, GETDATE())", comment);
    }

    // -------------------------------------------------------------------------
    // Project Members
    // -------------------------------------------------------------------------

    public async Task<List<ProjectMember>> GetMembersAsync(int projectId)
    {
        return (await _db.QueryAsync<ProjectMember>(
            "SELECT * FROM ProjectMembers WHERE ProjectId = @ProjectId ORDER BY Name",
            new { ProjectId = projectId })).ToList();
    }

    public async Task AddMemberAsync(ProjectMember member)
    {
        const string sql = @"
            INSERT INTO ProjectMembers (ProjectId, UserId, UserCode, Name, Email, Role, PortalAccess, CanApproveChanges, AddedAt)
            VALUES (@ProjectId, @UserId, @UserCode, @Name, @Email, @Role, @PortalAccess, @CanApproveChanges, GETDATE())";
        await _db.ExecuteObjAsync(sql, member);
    }

    public async Task UpdateMemberAsync(ProjectMember member)
    {
        const string sql = @"
            UPDATE ProjectMembers SET
                UserId = @UserId,
                UserCode = @UserCode,
                Name = @Name,
                Email = @Email,
                Role = @Role,
                PortalAccess = @PortalAccess,
                CanApproveChanges = @CanApproveChanges
            WHERE MemberId = @MemberId";
        await _db.ExecuteObjAsync(sql, member);
    }

    public async Task RemoveMemberAsync(int memberId)
    {
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM ProjectMembers WHERE MemberId = @MemberId",
            new() { ["MemberId"] = memberId });
    }

    // -------------------------------------------------------------------------
    // Milestones
    // -------------------------------------------------------------------------

    public async Task<List<ProjectMilestone>> GetMilestonesAsync(int projectId)
    {
        return (await _db.QueryAsync<ProjectMilestone>(
            "SELECT * FROM ProjectMilestones WHERE ProjectId = @ProjectId ORDER BY OrderIndex, DueDate",
            new { ProjectId = projectId })).ToList();
    }

    public async Task<int> AddMilestoneAsync(ProjectMilestone milestone)
    {
        const string sql = @"
            INSERT INTO ProjectMilestones (ProjectId, Title, Description, DueDate, ActualDate, Status, SignOffRequired, SignOffDate, SignOffBy, IsUAT, OrderIndex, CreatedAt)
            VALUES (@ProjectId, @Title, @Description, @DueDate, @ActualDate, @Status, @SignOffRequired, @SignOffDate, @SignOffBy, @IsUAT, @OrderIndex, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, milestone);
    }

    public async Task UpdateMilestoneAsync(ProjectMilestone milestone)
    {
        const string sql = @"
            UPDATE ProjectMilestones SET
                Title = @Title,
                Description = @Description,
                DueDate = @DueDate,
                ActualDate = @ActualDate,
                Status = @Status,
                SignOffRequired = @SignOffRequired,
                SignOffDate = @SignOffDate,
                SignOffBy = @SignOffBy,
                IsUAT = @IsUAT,
                OrderIndex = @OrderIndex
            WHERE MilestoneId = @MilestoneId";
        await _db.ExecuteObjAsync(sql, milestone);
    }

    public async Task DeleteMilestoneAsync(int milestoneId)
    {
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM ProjectMilestones WHERE MilestoneId = @MilestoneId",
            new() { ["MilestoneId"] = milestoneId });
    }

    public async Task SignOffMilestoneAsync(int milestoneId, string signedOffBy)
    {
        const string sql = @"
            UPDATE ProjectMilestones SET
                Status = 'Complete',
                SignOffDate = GETDATE(),
                SignOffBy = @SignedOffBy,
                ActualDate = CAST(GETDATE() AS DATE)
            WHERE MilestoneId = @MilestoneId";
        await _db.ExecuteNonQueryAsync(sql, new() { ["MilestoneId"] = milestoneId, ["SignedOffBy"] = signedOffBy });
    }

    // -------------------------------------------------------------------------
    // Cost Entries
    // -------------------------------------------------------------------------

    public async Task<List<ProjectCostEntry>> GetCostEntriesAsync(int projectId)
    {
        return (await _db.QueryAsync<ProjectCostEntry>(
            "SELECT * FROM ProjectCostEntries WHERE ProjectId = @ProjectId ORDER BY EntryDate DESC, CreatedAt DESC",
            new { ProjectId = projectId })).ToList();
    }

    public async Task<int> AddCostEntryAsync(ProjectCostEntry entry)
    {
        const string sql = @"
            INSERT INTO ProjectCostEntries (ProjectId, TaskId, EntryType, Description, Quantity, UnitRate, Amount, UserCode, EnteredBy, EntryDate, IsBillable, IsApproved, CreatedAt)
            VALUES (@ProjectId, @TaskId, @EntryType, @Description, @Quantity, @UnitRate, @Amount, @UserCode, @EnteredBy, @EntryDate, @IsBillable, @IsApproved, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, entry);
    }

    public async Task DeleteCostEntryAsync(int entryId)
    {
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM ProjectCostEntries WHERE CostEntryId = @CostEntryId",
            new() { ["CostEntryId"] = entryId });
    }

    public async Task<decimal> GetTotalCostAsync(int projectId)
    {
        var result = await _db.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT SUM(Amount) FROM ProjectCostEntries WHERE ProjectId = @ProjectId",
            new { ProjectId = projectId });
        return result ?? 0m;
    }

    // -------------------------------------------------------------------------
    // Change Requests
    // -------------------------------------------------------------------------

    public async Task<List<ChangeRequest>> GetChangeRequestsAsync(int projectId)
    {
        return (await _db.QueryAsync<ChangeRequest>(
            "SELECT * FROM ChangeRequests WHERE ProjectId = @ProjectId ORDER BY CreatedAt DESC",
            new { ProjectId = projectId })).ToList();
    }

    public async Task<int> CreateChangeRequestAsync(ChangeRequest cr)
    {
        const string sql = @"
            INSERT INTO ChangeRequests (ProjectId, ChangeNumber, Title, Description, ImpactDescription, ImpactDays, ImpactCost, Status, RaisedBy, RaisedByCode, RaisedAt, ApprovedBy, ApprovedAt, RejectionReason, LinkedQuoteId, LinkedInvoiceId, CreatedAt, UpdatedAt)
            VALUES (@ProjectId, @ChangeNumber, @Title, @Description, @ImpactDescription, @ImpactDays, @ImpactCost, @Status, @RaisedBy, @RaisedByCode, GETDATE(), @ApprovedBy, @ApprovedAt, @RejectionReason, @LinkedQuoteId, @LinkedInvoiceId, GETDATE(), GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, cr);
    }

    public async Task UpdateChangeRequestAsync(ChangeRequest cr)
    {
        const string sql = @"
            UPDATE ChangeRequests SET
                ChangeNumber = @ChangeNumber,
                Title = @Title,
                Description = @Description,
                ImpactDescription = @ImpactDescription,
                ImpactDays = @ImpactDays,
                ImpactCost = @ImpactCost,
                Status = @Status,
                ApprovedBy = @ApprovedBy,
                ApprovedAt = @ApprovedAt,
                RejectionReason = @RejectionReason,
                LinkedQuoteId = @LinkedQuoteId,
                LinkedInvoiceId = @LinkedInvoiceId,
                UpdatedAt = GETDATE()
            WHERE ChangeRequestId = @ChangeRequestId";
        await _db.ExecuteObjAsync(sql, cr);
    }

    public async Task UpdateChangeRequestStatusAsync(int id, string status, string? by = null, string? reason = null)
    {
        const string sql = @"
            UPDATE ChangeRequests SET
                Status = @Status,
                ApprovedBy   = CASE WHEN @Status IN ('Approved','Rejected') THEN @By   ELSE ApprovedBy   END,
                ApprovedAt   = CASE WHEN @Status = 'Approved'               THEN GETDATE() ELSE ApprovedAt END,
                RejectionReason = CASE WHEN @Status = 'Rejected'            THEN @Reason ELSE RejectionReason END,
                UpdatedAt = GETDATE()
            WHERE ChangeRequestId = @Id";
        await _db.ExecuteNonQueryAsync(sql, new() { ["Id"] = id, ["Status"] = status, ["By"] = by, ["Reason"] = reason });
    }

    // -------------------------------------------------------------------------
    // Risks & Issues
    // -------------------------------------------------------------------------

    public async Task<List<RiskIssue>> GetRisksIssuesAsync(int projectId, string? type = null)
    {
        var sql = "SELECT * FROM RisksIssues WHERE ProjectId = @ProjectId";
        if (!string.IsNullOrEmpty(type)) sql += " AND ItemType = @Type";
        sql += " ORDER BY Priority DESC, CreatedAt DESC";
        return (await _db.QueryAsync<RiskIssue>(sql, new { ProjectId = projectId, Type = type })).ToList();
    }

    public async Task<int> AddRiskIssueAsync(RiskIssue item)
    {
        const string sql = @"
            INSERT INTO RisksIssues (ProjectId, ItemType, Title, Description, Likelihood, Impact, Priority, Status, OwnedBy, OwnedByCode, MitigationPlan, AssignedTaskId, DueDate, CreatedAt, UpdatedAt)
            VALUES (@ProjectId, @ItemType, @Title, @Description, @Likelihood, @Impact, @Priority, @Status, @OwnedBy, @OwnedByCode, @MitigationPlan, @AssignedTaskId, @DueDate, GETDATE(), GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, item);
    }

    public async Task UpdateRiskIssueAsync(RiskIssue item)
    {
        const string sql = @"
            UPDATE RisksIssues SET
                ItemType = @ItemType,
                Title = @Title,
                Description = @Description,
                Likelihood = @Likelihood,
                Impact = @Impact,
                Priority = @Priority,
                Status = @Status,
                OwnedBy = @OwnedBy,
                OwnedByCode = @OwnedByCode,
                MitigationPlan = @MitigationPlan,
                AssignedTaskId = @AssignedTaskId,
                DueDate = @DueDate,
                UpdatedAt = GETDATE()
            WHERE RiskIssueId = @RiskIssueId";
        await _db.ExecuteObjAsync(sql, item);
    }

    public async Task DeleteRiskIssueAsync(int id)
    {
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM RisksIssues WHERE RiskIssueId = @RiskIssueId",
            new() { ["RiskIssueId"] = id });
    }

    // -------------------------------------------------------------------------
    // Project Health Snapshot
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes a health snapshot for the project, including velocity-based schedule prediction,
    /// budget variance, and RAG status for schedule and budget.
    /// Also persists the predicted end date back to the Projects table.
    /// </summary>
    public async Task<ProjectHealthSnapshot> GetProjectHealthAsync(int projectId)
    {
        var project = await GetProjectAsync(projectId)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var tasks        = await GetTasksAsync(projectId: projectId);
        var costEntries  = await GetCostEntriesAsync(projectId);
        var risksIssues  = await GetRisksIssuesAsync(projectId);
        var changeReqs   = await GetChangeRequestsAsync(projectId);
        var milestones   = await GetMilestonesAsync(projectId);

        var today = DateTime.Today;

        // Task counts
        int tasksTotal   = tasks.Count;
        int tasksDone    = tasks.Count(t => t.Status == "Done");
        int tasksOverdue = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != "Done");

        // Percent complete: average of task PercentComplete, or 0 if no tasks
        int percentComplete = tasksTotal > 0
            ? (int)Math.Round(tasks.Average(t => (double)t.PercentComplete))
            : 0;

        // Velocity: % per week elapsed since project start
        decimal velocity = 0m;
        if (project.StartDate.HasValue)
        {
            var weeksElapsed = (today - project.StartDate.Value.Date).TotalDays / 7.0;
            if (weeksElapsed >= 2.0 && percentComplete > 0)
                velocity = (decimal)(percentComplete / weeksElapsed);
        }

        // Predicted end date
        DateTime? predictedEndDate;
        if (percentComplete == 0 || velocity == 0)
        {
            predictedEndDate = project.EndDate;
        }
        else
        {
            var remainingPercent = 100 - percentComplete;
            var weeksRemaining   = (double)(remainingPercent / velocity);
            predictedEndDate     = today.AddDays(weeksRemaining * 7);
        }

        // Budget
        var totalCost       = costEntries.Sum(c => c.Amount);
        var budgetAmount    = project.BudgetAmount ?? 0m;
        var budgetVariance  = budgetAmount - totalCost;

        // Schedule health
        string scheduleHealth;
        int? daysVariance = null;
        if (predictedEndDate.HasValue && project.EndDate.HasValue)
        {
            daysVariance = (int)(predictedEndDate.Value.Date - project.EndDate.Value.Date).TotalDays;
            scheduleHealth = daysVariance <= 7  ? "OnTrack"
                           : daysVariance <= 21 ? "AtRisk"
                           :                      "Delayed";
        }
        else
        {
            scheduleHealth = "OnTrack";
        }

        // Budget health
        string budgetHealth;
        if (budgetAmount <= 0)
        {
            budgetHealth = "OnTrack";
        }
        else if (totalCost <= budgetAmount * 0.9m)
        {
            budgetHealth = "OnTrack";
        }
        else if (totalCost <= budgetAmount)
        {
            budgetHealth = "AtRisk";
        }
        else
        {
            budgetHealth = "OverBudget";
        }

        // Persist predicted end date
        await _db.ExecuteNonQueryAsync(
            "UPDATE Projects SET PredictedEndDate = @PredictedEndDate, UpdatedAt = GETDATE() WHERE ProjectId = @ProjectId",
            new() { ["PredictedEndDate"] = predictedEndDate, ["ProjectId"] = projectId });

        return new ProjectHealthSnapshot
        {
            ProjectId              = projectId,
            PercentComplete        = percentComplete,
            TasksTotal             = tasksTotal,
            TasksDone              = tasksDone,
            TasksOverdue           = tasksOverdue,
            BudgetAmount           = budgetAmount,
            ActualCost             = totalCost,
            BudgetVariance         = budgetVariance,
            PredictedEndDate       = predictedEndDate,
            PlannedEndDate         = project.EndDate,
            DaysVariance           = daysVariance,
            ScheduleHealth         = scheduleHealth,
            BudgetHealth           = budgetHealth,
            OpenRisks              = risksIssues.Count(r => r.ItemType == "Risk"  && r.Status != "Closed"),
            OpenIssues             = risksIssues.Count(r => r.ItemType == "Issue" && r.Status != "Closed"),
            PendingChangeRequests  = changeReqs.Count(c => c.Status == "PendingApproval"),
            MilestonesOverdue      = milestones.Count(m => m.DueDate.Date < today && m.Status != "Complete"),
            CompletionVelocity     = velocity
        };
    }
}
