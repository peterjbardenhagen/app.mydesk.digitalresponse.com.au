namespace MyDesk.Shared.Models;

/// <summary>
/// A user-defined scheduled task — runs an <see cref="ScheduledTaskActionType"/> with
/// supplied parameters on a recurring schedule. Tenant-scoped.
///
/// Persisted in the <c>ScheduledTasks</c> SQL table; executed by Hangfire's recurring
/// job scheduler. The Hangfire job id is <c>tenant-{TenantId:N}-task-{ScheduledTaskId}</c>.
/// </summary>
public class ScheduledTask
{
    public int ScheduledTaskId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>What the task does — see <see cref="ScheduledTaskActionType"/>.</summary>
    public string ActionType { get; set; } = nameof(ScheduledTaskActionType.AskAi);

    /// <summary>JSON blob of action-specific parameters (prompt, recipient, etc.).</summary>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>Recurrence pattern — see <see cref="ScheduleRecurrence"/>.</summary>
    public string Recurrence { get; set; } = nameof(ScheduleRecurrence.Daily);

    /// <summary>0..23 hour-of-day for Daily/Weekly/Monthly schedules (local time).</summary>
    public int HourOfDay { get; set; } = 9;

    /// <summary>0..59 minute-of-hour for Daily/Weekly/Monthly schedules.</summary>
    public int MinuteOfHour { get; set; } = 0;

    /// <summary>0..6 day-of-week for Weekly (0=Sun..6=Sat).</summary>
    public int? DayOfWeek { get; set; }

    /// <summary>1..31 day-of-month for Monthly.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Free-form cron expression — used when Recurrence == Cron.</summary>
    public string? CronExpression { get; set; }

    /// <summary>IANA / Windows time zone id (eg. "AUS Eastern Standard Time"). Defaults to platform setting.</summary>
    public string? TimeZoneId { get; set; }

    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public string? LastStatus { get; set; }     // "Success" | "Error"
    public string? LastResult { get; set; }     // truncated result body / error
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public enum ScheduledTaskActionType
{
    /// <summary>Send a prompt to Ask AI and email the response to one or more recipients.</summary>
    AskAi,
    /// <summary>Generate a built-in report (sales / pipeline / outstanding / etc.) and email it.</summary>
    EmailReport,
    /// <summary>Send a static templated email with optional dynamic placeholders.</summary>
    SendEmail,
    /// <summary>Call an external HTTP endpoint (webhook style).</summary>
    HttpCall,
}

public enum ScheduleRecurrence
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    /// <summary>Use the supplied <see cref="ScheduledTask.CronExpression"/>.</summary>
    Cron,
}

/// <summary>One row of execution history for a <see cref="ScheduledTask"/>.</summary>
public class ScheduledTaskRun
{
    public int ScheduledTaskRunId { get; set; }
    public int ScheduledTaskId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "Running";  // Running | Success | Error
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
