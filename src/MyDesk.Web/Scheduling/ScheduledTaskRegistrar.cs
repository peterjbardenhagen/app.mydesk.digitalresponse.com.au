using Hangfire;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Scheduling;

/// <summary>
/// Adapts <see cref="IScheduledTaskRegistrar"/> to Hangfire's recurring-job API.
/// Job ids are stable per tenant + task so updates are idempotent.
/// </summary>
public class ScheduledTaskRegistrar : IScheduledTaskRegistrar
{
    private readonly IRecurringJobManager _jobs;

    public ScheduledTaskRegistrar(IRecurringJobManager jobs)
    {
        _jobs = jobs;
    }

    public static string JobIdFor(ScheduledTask t) => $"tenant-{t.TenantId:N}-task-{t.ScheduledTaskId}";

    public Task AddOrUpdateAsync(ScheduledTask task)
    {
        var cron = ScheduledTaskService.BuildCron(task);
        var tz = ResolveTimeZone(task.TimeZoneId);

        // Hangfire serialises only the args — the job resolves the task at execution time
        // (so DB updates take effect on next fire without re-registering).
        _jobs.AddOrUpdate<ScheduledTaskExecutor>(
            recurringJobId: JobIdFor(task),
            methodCall: x => x.RunAsync(task.ScheduledTaskId, task.TenantId.ToString(), null!),
            cronExpression: cron,
            options: new RecurringJobOptions { TimeZone = tz });

        return Task.CompletedTask;
    }

    public Task RemoveAsync(ScheduledTask task)
    {
        _jobs.RemoveIfExists(JobIdFor(task));
        return Task.CompletedTask;
    }

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Local;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Local; }
    }
}
