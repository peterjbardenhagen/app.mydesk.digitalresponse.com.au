using System.Text.Json;
using MyDesk.Shared.Services;
using MyDesk.Web.Services.Legal;

namespace MyDesk.Web.AI.Tools.Legal;

/// <summary>
/// AI tool that queries Radix timesheets. Only active when Radix is configured
/// (EnableLegalModules + RadixApiUrl + RadixApiKey set in PlatformSettings).
/// </summary>
public class RadixTimesheetsTool : IAiTool
{
    private readonly RadixService _radix;
    private readonly PlatformSettingsService _settings;

    public RadixTimesheetsTool(RadixService radix, PlatformSettingsService settings)
    {
        _radix = radix;
        _settings = settings;
    }

    public string Name => "get_radix_timesheets";

    public string Description =>
        "Queries timesheet entries from the Radix legal practice management system. " +
        "Returns billable time entries (fee earner, matter, units, hours, WIP amount, narrative). " +
        "1 unit = 6 minutes. Use when the user asks about legal timesheets, billable hours, WIP, " +
        "fee earner activity, or matter time. Only available for firms using Radix.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "from":          { "type": "string", "description": "Start date (YYYY-MM-DD). Defaults to 30 days ago." },
            "to":            { "type": "string", "description": "End date (YYYY-MM-DD). Defaults to today." },
            "fee_earner":    { "type": "string", "description": "Filter by fee earner code (optional)." },
            "summary_only":  { "type": "boolean", "description": "Return totals only (units, hours, WIP) rather than individual rows." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!_settings.Current.EnableLegalModules || !_radix.IsConfigured)
        {
            return new AiToolResult(
                """{"error":"Radix is not configured","hint":"Set RadixApiUrl and RadixApiKey in Platform Settings and enable EnableLegalModules."}""",
                new AiNoticeSpec("Radix Not Configured",
                    "Radix API integration is not configured for this tenant.",
                    "warning"));
        }

        var fromStr = args.TryGetProperty("from", out var f) ? f.GetString() : null;
        var toStr   = args.TryGetProperty("to",   out var t) ? t.GetString() : null;
        var earner  = args.TryGetProperty("fee_earner", out var e) ? e.GetString() : null;
        var summaryOnly = args.TryGetProperty("summary_only", out var s) && s.GetBoolean();

        var from = DateTime.TryParse(fromStr, out var fd) ? fd : DateTime.Today.AddDays(-30);
        var to   = DateTime.TryParse(toStr,   out var td) ? td : DateTime.Today;

        var sheets = await _radix.GetTimesheetsAsync(from, to, earner, ct);

        if (sheets.Count == 0)
        {
            return new AiToolResult(
                JsonSerializer.Serialize(new { found = 0, from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd") }),
                new AiNoticeSpec("Radix Timesheets", $"No timesheet entries found between {from:dd/MM/yyyy} and {to:dd/MM/yyyy}.", "info"));
        }

        var totalUnits = sheets.Sum(x => x.Units);
        var totalHours = Math.Round(totalUnits / 10m, 1);
        var totalWip   = sheets.Sum(x => x.WipAmount);

        if (summaryOnly)
        {
            return new AiToolResult(
                JsonSerializer.Serialize(new { found = sheets.Count, totalUnits, totalHours, totalWip, from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd") }),
                new AiNoticeSpec("Radix Timesheets Summary",
                    $"{sheets.Count} entries | {totalHours} hrs ({totalUnits} units) | WIP ${totalWip:N2}",
                    "info"));
        }

        var columns = new[] { "Date", "Fee Earner", "Matter", "Units", "Hours", "WIP ($)", "Activity", "Status" };
        var rows = sheets.Take(50).Select(x => new[]
        {
            x.Date.ToString("dd/MM/yyyy"),
            $"{x.FeeEarnerCode} {x.FeeEarnerName}".Trim(),
            $"{x.MatterNumber} {x.MatterName}".Trim(),
            x.Units.ToString(),
            (x.Units / 10m).ToString("F1"),
            x.WipAmount.ToString("N2"),
            x.ActivityCode,
            x.Status,
        }).ToArray();

        var title = $"Radix Timesheets: {from:dd/MM/yy}–{to:dd/MM/yy} ({sheets.Count} entries, {totalHours} hrs, WIP ${totalWip:N2})";

        return new AiToolResult(
            JsonSerializer.Serialize(new { found = sheets.Count, totalUnits, totalHours, totalWip }),
            new AiTableSpec(title, columns, rows));
    }
}
