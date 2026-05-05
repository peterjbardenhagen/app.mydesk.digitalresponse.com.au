using System.Text.Json;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Lets the assistant create a recurring scheduled task — typically an Ask AI
/// prompt or report email — directly from natural-language requests like
/// "send me a weekly pipeline summary every Monday at 9am".
///
/// Uses <see cref="ScheduledTaskService"/> so it goes through the same validation
/// and Hangfire registration path as the manual admin UI.
/// </summary>
public class ScheduleReportTool : IAiTool
{
    private readonly ScheduledTaskService _tasks;

    public ScheduleReportTool(ScheduledTaskService tasks) { _tasks = tasks; }

    public string Name => "schedule_recurring_task";

    public string Description =>
        "Schedule a recurring task in MyDesk's task scheduler. Use for natural language like " +
        "'send me a weekly pipeline summary every Monday 9am' (action=ask_ai with a prompt + emailTo) " +
        "or 'email me the monthly invoice report'. Returns confirmation + a notice card.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "required": ["name","action","recurrence"],
        "properties": {
            "name":        { "type": "string", "description": "Short name for the task." },
            "description": { "type": "string", "description": "Optional human description." },
            "action":      { "type": "string", "enum": ["ask_ai","email_report","send_email","http_call"], "description": "What the task does." },
            "recurrence":  { "type": "string", "enum": ["hourly","daily","weekly","monthly","cron"] },
            "hour":        { "type": "integer", "minimum": 0, "maximum": 23 },
            "minute":      { "type": "integer", "minimum": 0, "maximum": 59 },
            "dayOfWeek":   { "type": "integer", "minimum": 0, "maximum": 6, "description": "0=Sun .. 6=Sat" },
            "dayOfMonth":  { "type": "integer", "minimum": 1, "maximum": 31 },
            "cronExpression": { "type": "string", "description": "Used when recurrence == 'cron'." },

            "prompt":   { "type": "string", "description": "AI prompt (action=ask_ai)." },
            "emailTo":  { "type": "string", "description": "Recipient email." },
            "subject":  { "type": "string", "description": "Email subject." },
            "body":     { "type": "string", "description": "Email body (action=send_email)." },
            "report":   { "type": "string", "description": "Report key (action=email_report)." },
            "url":      { "type": "string", "description": "URL (action=http_call)." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement a, CancellationToken ct = default)
    {
        string get(string k, string fallback = "") =>
            a.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;
        int? getInt(string k) =>
            a.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

        var actionRaw = get("action", "ask_ai");
        var action = actionRaw switch
        {
            "ask_ai"        => nameof(ScheduledTaskActionType.AskAi),
            "email_report"  => nameof(ScheduledTaskActionType.EmailReport),
            "send_email"    => nameof(ScheduledTaskActionType.SendEmail),
            "http_call"     => nameof(ScheduledTaskActionType.HttpCall),
            _               => nameof(ScheduledTaskActionType.AskAi)
        };
        var recRaw = get("recurrence", "weekly");
        var recurrence = char.ToUpperInvariant(recRaw[0]) + recRaw.Substring(1).ToLowerInvariant();

        // Build the per-action parameters JSON.
        var parameters = action switch
        {
            nameof(ScheduledTaskActionType.AskAi) => new Dictionary<string, object?>
            {
                ["prompt"]  = get("prompt"),
                ["emailTo"] = get("emailTo"),
                ["subject"] = get("subject", $"[MyDesk] {get("name")}"),
            },
            nameof(ScheduledTaskActionType.EmailReport) => new Dictionary<string, object?>
            {
                ["report"]  = get("report", "weekly-summary"),
                ["emailTo"] = get("emailTo"),
                ["subject"] = get("subject", $"[MyDesk] {get("name")}"),
            },
            nameof(ScheduledTaskActionType.SendEmail) => new Dictionary<string, object?>
            {
                ["to"]      = get("emailTo"),
                ["subject"] = get("subject", $"[MyDesk] {get("name")}"),
                ["body"]    = get("body"),
            },
            nameof(ScheduledTaskActionType.HttpCall) => new Dictionary<string, object?>
            {
                ["url"]    = get("url"),
                ["method"] = "GET",
            },
            _ => new Dictionary<string, object?>()
        };

        var task = new ScheduledTask
        {
            Name = get("name", "Untitled Ask AI task"),
            Description = get("description"),
            ActionType = action,
            Recurrence = recurrence,
            HourOfDay = getInt("hour") ?? 9,
            MinuteOfHour = getInt("minute") ?? 0,
            DayOfWeek = getInt("dayOfWeek"),
            DayOfMonth = getInt("dayOfMonth"),
            CronExpression = get("cronExpression"),
            ParametersJson = JsonSerializer.Serialize(parameters),
            IsEnabled = true,
        };

        try
        {
            var id = await _tasks.CreateAsync(task);
            var summary = new
            {
                ok = true,
                taskId = id,
                name = task.Name,
                action,
                recurrence,
                cron = ScheduledTaskService.BuildCron(task),
                emailTo = get("emailTo"),
            };
            var notice = new AiNoticeSpec(
                Title: "Scheduled task created",
                Message: $"'{task.Name}' will run on schedule '{ScheduledTaskService.BuildCron(task)}'." +
                         (string.IsNullOrWhiteSpace(get("emailTo")) ? "" : $" Output emailed to {get("emailTo")}."),
                Severity: "success");
            return new AiToolResult(JsonSerializer.Serialize(summary), notice);
        }
        catch (Exception ex)
        {
            var error = new { ok = false, error = ex.Message };
            return new AiToolResult(
                JsonSerializer.Serialize(error),
                new AiNoticeSpec("Could not schedule task", ex.Message, "error"));
        }
    }
}
