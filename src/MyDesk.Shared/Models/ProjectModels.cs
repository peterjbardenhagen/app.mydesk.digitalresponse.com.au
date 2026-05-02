using System;
using System.Collections.Generic;

namespace MyDesk.Shared.Models;

public class Project
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string Priority { get; set; } = "Medium";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Sprint
{
    public int SprintId { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Planning";
    public string? Goal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaskItem
{
    public int TaskId { get; set; }
    public int? ProjectId { get; set; }
    public int? SprintId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public int? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string Status { get; set; } = "Backlog";
    public string Priority { get; set; } = "Medium";
    public int? StoryPoints { get; set; }
    public string TaskType { get; set; } = "Task";
    public string? Tags { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public int? ParentTaskId { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CapacityPlanning
{
    public int CapacityId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime WeekStartDate { get; set; }
    public decimal AvailableHours { get; set; } = 40;
    public decimal AllocatedHours { get; set; } = 0;
    public decimal LeaveHours { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaskComment
{
    public int CommentId { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
