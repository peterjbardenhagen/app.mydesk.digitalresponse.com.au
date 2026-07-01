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
    public string CommercialModel { get; set; } = "FixedPrice"; // FixedPrice, TimeAndMaterials, Milestone, Retainer
    public int? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }
    public int? LinkedQuoteId { get; set; }
    public int? LinkedInvoiceId { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal? ActualCost { get; set; } // computed/denorm
    public int PercentComplete { get; set; } // 0-100, computed from tasks
    public DateTime? PredictedEndDate { get; set; } // ML prediction
    public decimal? PredictedFinalCost { get; set; } // ML prediction
    public string? ProjectCode { get; set; }
    public string? ClientProjectManager { get; set; } // client-side PM name
    public string? PortalToken { get; set; } // for client/supplier portal access (GUID)
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
    public int PercentComplete { get; set; } // 0-100
    public DateTime? TaskStartDate { get; set; } // planned start (for Gantt)
    public DateTime? TaskEndDate { get; set; } // planned end (for Gantt)
    public int? MilestoneId { get; set; }
    public string? MilestoneName { get; set; }
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

public class ProjectMember
{
    public int MemberId { get; set; }
    public int ProjectId { get; set; }
    public int? UserId { get; set; }
    public string UserCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string Role { get; set; } = "TeamMember"; // ProjectManager, TeamMember, Client, Supplier, Stakeholder
    public bool PortalAccess { get; set; }
    public bool CanApproveChanges { get; set; }
    public DateTime AddedAt { get; set; }
}

public class ProjectMilestone
{
    public int MilestoneId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Complete, Overdue
    public bool SignOffRequired { get; set; }
    public DateTime? SignOffDate { get; set; }
    public string? SignOffBy { get; set; }
    public bool IsUAT { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectCostEntry
{
    public int CostEntryId { get; set; }
    public int ProjectId { get; set; }
    public int? TaskId { get; set; }
    public string EntryType { get; set; } = "Labour"; // Labour, Expense, Material, Subcontractor
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; }
    public decimal Amount { get; set; }
    public string? UserCode { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime EntryDate { get; set; }
    public bool IsBillable { get; set; } = true;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeRequest
{
    public int ChangeRequestId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ImpactDescription { get; set; }
    public int? ImpactDays { get; set; }
    public decimal? ImpactCost { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Approved, Rejected, Invoiced
    public string RaisedBy { get; set; } = "";
    public string? RaisedByCode { get; set; }
    public DateTime RaisedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? LinkedQuoteId { get; set; }
    public int? LinkedInvoiceId { get; set; }
    public string ChangeNumber { get; set; } = ""; // e.g. CR-001
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RiskIssue
{
    public int RiskIssueId { get; set; }
    public int ProjectId { get; set; }
    public string ItemType { get; set; } = "Risk"; // Risk, Issue
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Likelihood { get; set; } = "Medium"; // Low, Medium, High (risks only)
    public string Impact { get; set; } = "Medium"; // Low, Medium, High
    public string Priority { get; set; } = "Medium"; // computed or manual
    public string Status { get; set; } = "Open"; // Open, InProgress, Closed, Accepted
    public string? OwnedBy { get; set; }
    public string? OwnedByCode { get; set; }
    public string? MitigationPlan { get; set; }
    public int? AssignedTaskId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectHealthSnapshot
{
    public int ProjectId { get; set; }
    public int PercentComplete { get; set; }
    public int TasksTotal { get; set; }
    public int TasksDone { get; set; }
    public int TasksOverdue { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualCost { get; set; }
    public decimal BudgetVariance { get; set; } // positive = under budget
    public DateTime? PredictedEndDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? DaysVariance { get; set; } // negative = late
    public string ScheduleHealth { get; set; } = "OnTrack"; // OnTrack, AtRisk, Delayed
    public string BudgetHealth { get; set; } = "OnTrack";
    public int OpenRisks { get; set; }
    public int OpenIssues { get; set; }
    public int PendingChangeRequests { get; set; }
    public int MilestonesOverdue { get; set; }
    public decimal CompletionVelocity { get; set; } // % per week
}
