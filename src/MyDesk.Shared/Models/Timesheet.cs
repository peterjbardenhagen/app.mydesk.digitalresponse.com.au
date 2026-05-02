using System;
using System.Collections.Generic;

namespace MyDesk.Shared.Models;

public class Timesheet
{
    public int TimesheetId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime WeekStartDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedTo { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
    public string? ManagerNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public List<TimesheetEntry> Entries { get; set; } = new();
}

public class TimesheetEntry
{
    public int TimesheetEntryId { get; set; }
    public int TimesheetId { get; set; }
    public DateTime EntryDate { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public string TimeType { get; set; } = "Billable"; // Billable, Non-Billable
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? Description { get; set; }
    public string? TaskName { get; set; }
}

public class TimesheetSummary
{
    public int TimesheetId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime WeekStartDate { get; set; }
    public string Status { get; set; } = "";
    public int TotalHours { get; set; }
    public int BillableHours { get; set; }
    public int NonBillableHours { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
