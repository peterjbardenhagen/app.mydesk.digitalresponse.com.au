using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ProjectService
{
    private readonly DatabaseService _db;

    public ProjectService(DatabaseService db)
    {
        _db = db;
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
            END";
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
    }

    // Projects
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

    // Sprints
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

    // Tasks
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

    public async Task DeleteTaskAsync(int taskId)
    {
        await _db.ExecuteObjAsync("DELETE FROM Tasks WHERE TaskId = @TaskId", new { TaskId = taskId });
    }

    // Capacity Planning
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

    // Task Comments
    public async Task<List<TaskComment>> GetTaskCommentsAsync(int taskId)
    {
        return (await _db.QueryAsync<TaskComment>("SELECT * FROM TaskComments WHERE TaskId = @TaskId ORDER BY CreatedAt ASC", new { TaskId = taskId })).ToList();
    }

    public async Task AddTaskCommentAsync(TaskComment comment)
    {
        await _db.ExecuteObjAsync("INSERT INTO TaskComments (TaskId, UserId, UserName, Comment, CreatedAt) VALUES (@TaskId, @UserId, @UserName, @Comment, GETDATE())", comment);
    }
}
