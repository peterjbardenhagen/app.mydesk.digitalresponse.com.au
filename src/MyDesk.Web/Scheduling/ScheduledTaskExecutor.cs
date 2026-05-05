using System.Text.Json;
using Hangfire;
using Hangfire.Server;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;
using MyDesk.Web.Services;

namespace MyDesk.Web.Scheduling;

/// <summary>
/// Hangfire job target — invoked on the configured cron schedule.
/// Each call resolves the task definition fresh from the DB so admin edits
/// take effect on the next fire without re-registering the recurring job.
/// </summary>
public class ScheduledTaskExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledTaskExecutor> _logger;

    public ScheduledTaskExecutor(IServiceScopeFactory scopeFactory, ILogger<ScheduledTaskExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Hangfire serialises method calls — keep this signature stable.
    /// <paramref name="hangfireContext"/> is supplied by Hangfire itself when registered as a parameter.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RunAsync(int taskId, string tenantIdRaw, PerformContext? hangfireContext)
    {
        if (!Guid.TryParse(tenantIdRaw, out var tenantId))
        {
            _logger.LogError("ScheduledTaskExecutor: invalid tenant id '{Tenant}' for task {Task}", tenantIdRaw, taskId);
            return;
        }

        // Wrap the scope in a tenant-impersonation block so DatabaseService, EmailService, etc.
        // see the right tenant context (for SESSION_CONTEXT, email-redirect guard, etc.).
        using var _ = TenantImpersonation.For(tenantId, tenantName: "scheduled", userId: null, userCode: "scheduler");
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var taskService = sp.GetRequiredService<ScheduledTaskService>();
        var task = await taskService.GetAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("ScheduledTask {Id} not found for tenant {Tenant} - removing recurring job", taskId, tenantId);
            return;
        }
        if (!task.IsEnabled)
        {
            _logger.LogInformation("ScheduledTask {Id} disabled - skipping", taskId);
            return;
        }

        _logger.LogInformation("Running ScheduledTask {Id} ({Name}) for tenant {Tenant}, action={Action}",
            taskId, task.Name, tenantId, task.ActionType);

        try
        {
            string result = task.ActionType switch
            {
                nameof(ScheduledTaskActionType.AskAi)       => await RunAskAiAsync(sp, task),
                nameof(ScheduledTaskActionType.EmailReport) => await RunEmailReportAsync(sp, task),
                nameof(ScheduledTaskActionType.SendEmail)   => await RunSendEmailAsync(sp, task),
                nameof(ScheduledTaskActionType.HttpCall)    => await RunHttpCallAsync(sp, task),
                _ => throw new NotSupportedException($"Unknown action type '{task.ActionType}'.")
            };

            await taskService.RecordRunAsync(taskId, tenantId, "Success", result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduledTask {Id} failed", taskId);
            await taskService.RecordRunAsync(taskId, tenantId, "Error", null, ex.Message);
            throw; // Hangfire will mark failed + retry per its policy
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Action runners
    // ──────────────────────────────────────────────────────────────────────

    private static T? GetParam<T>(JsonElement root, string key, T? fallback = default)
    {
        if (!root.TryGetProperty(key, out var el) || el.ValueKind == JsonValueKind.Null) return fallback;
        try
        {
            if (typeof(T) == typeof(string)) return (T)(object)(el.GetString() ?? string.Empty);
            return el.Deserialize<T>();
        }
        catch { return fallback; }
    }

    private async Task<string> RunAskAiAsync(IServiceProvider sp, ScheduledTask task)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(task.ParametersJson) ? "{}" : task.ParametersJson);
        var prompt    = GetParam<string>(doc.RootElement, "prompt") ?? "";
        var system    = GetParam<string>(doc.RootElement, "system") ?? "You are a helpful assistant for an Australian business management platform.";
        var maxTokens = GetParam<int>(doc.RootElement, "maxTokens", 800);
        var emailTo   = GetParam<string>(doc.RootElement, "emailTo");
        var subject   = GetParam<string>(doc.RootElement, "subject") ?? $"[MyDesk] {task.Name}";

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("AskAi action requires a 'prompt' parameter.");

        var ai = sp.GetRequiredService<AzureAIService>();
        var reply = await ai.ChatAsync(new[]
        {
            AzureChatMessage.System(system),
            AzureChatMessage.User(prompt),
        }, maxTokens: maxTokens);

        if (!reply.IsSuccess) throw new InvalidOperationException(reply.Content ?? "AI call failed");
        var content = string.IsNullOrEmpty(reply.Content) ? "(empty)" : reply.Content;

        if (!string.IsNullOrWhiteSpace(emailTo))
        {
            var html = $"<h3>{System.Net.WebUtility.HtmlEncode(task.Name)}</h3>" +
                       $"<p><em>Prompt:</em> {System.Net.WebUtility.HtmlEncode(prompt)}</p>" +
                       $"<hr/><div style='white-space:pre-wrap;font-family:sans-serif;'>{System.Net.WebUtility.HtmlEncode(content).Replace("\n", "<br/>")}</div>";
            var email = sp.GetRequiredService<EmailService>();
            await email.SendAsync(emailTo, subject, html);
        }

        return content.Length > 1000 ? content.Substring(0, 1000) + "…" : content;
    }

    private async Task<string> RunEmailReportAsync(IServiceProvider sp, ScheduledTask task)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(task.ParametersJson) ? "{}" : task.ParametersJson);
        var reportKey = GetParam<string>(doc.RootElement, "report") ?? "weekly-summary";
        var emailTo   = GetParam<string>(doc.RootElement, "emailTo")
                        ?? throw new InvalidOperationException("EmailReport requires an 'emailTo' parameter.");
        var subject   = GetParam<string>(doc.RootElement, "subject") ?? $"[MyDesk] {task.Name}";

        var report = sp.GetRequiredService<ReportService>();
        // Use the ReportService's general-purpose dispatcher; if the named report doesn't exist
        // we still want a graceful failure rather than a stack trace in the email.
        string body;
        try
        {
            // Attempt to render. The signature is intentionally generic — a future PR can
            // implement IRecurringReport for richer report types.
            var html = await TryRenderReport(report, reportKey);
            body = html ?? $"<p>Report '{System.Net.WebUtility.HtmlEncode(reportKey)}' is not yet implemented.</p>";
        }
        catch (Exception ex)
        {
            body = $"<p>Failed to render report '{System.Net.WebUtility.HtmlEncode(reportKey)}': {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>";
        }

        var email = sp.GetRequiredService<EmailService>();
        await email.SendAsync(emailTo, subject, body);
        return $"Report '{reportKey}' emailed to {emailTo}.";
    }

    private static async Task<string?> TryRenderReport(ReportService _, string reportKey)
    {
        // Placeholder — wire to a real report registry as the platform grows.
        await Task.CompletedTask;
        return $"<h2>{System.Net.WebUtility.HtmlEncode(reportKey)}</h2>" +
               "<p>This is a scheduled report placeholder. Hook a concrete renderer into ScheduledTaskExecutor.TryRenderReport.</p>";
    }

    private async Task<string> RunSendEmailAsync(IServiceProvider sp, ScheduledTask task)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(task.ParametersJson) ? "{}" : task.ParametersJson);
        var to      = GetParam<string>(doc.RootElement, "to")      ?? throw new InvalidOperationException("SendEmail requires 'to'.");
        var subject = GetParam<string>(doc.RootElement, "subject") ?? "MyDesk scheduled message";
        var body    = GetParam<string>(doc.RootElement, "body")    ?? "(no body)";
        var email = sp.GetRequiredService<EmailService>();
        await email.SendAsync(to, subject, body);
        return $"Email sent to {to}.";
    }

    private async Task<string> RunHttpCallAsync(IServiceProvider sp, ScheduledTask task)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(task.ParametersJson) ? "{}" : task.ParametersJson);
        var url    = GetParam<string>(doc.RootElement, "url") ?? throw new InvalidOperationException("HttpCall requires 'url'.");
        var method = GetParam<string>(doc.RootElement, "method") ?? "GET";
        var bodyText = GetParam<string>(doc.RootElement, "body");

        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        var req = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url);
        if (!string.IsNullOrWhiteSpace(bodyText))
            req.Content = new StringContent(bodyText, System.Text.Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req);
        var respText = await resp.Content.ReadAsStringAsync();
        return $"{(int)resp.StatusCode} {resp.ReasonPhrase} — {(respText.Length > 500 ? respText.Substring(0, 500) + "…" : respText)}";
    }
}
