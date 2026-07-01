using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Searches data across Composio-connected third-party apps:
/// QuickBooks Online, OneDrive, and any other authenticated integrations.
/// Resolves API key: user's own Composio key first, then platform-level key.
/// </summary>
public class SearchComposioAppsTool : IAiTool
{
    private readonly ComposioIntegrationService _composio;
    private readonly UserIntelligenceService _intelligence;
    private readonly IHttpContextAccessor _httpCtx;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SearchComposioAppsTool> _logger;

    public SearchComposioAppsTool(
        ComposioIntegrationService composio,
        UserIntelligenceService intelligence,
        IHttpContextAccessor httpCtx,
        IHttpClientFactory httpFactory,
        ILogger<SearchComposioAppsTool> logger)
    {
        _composio     = composio;
        _intelligence = intelligence;
        _httpCtx      = httpCtx;
        _httpFactory  = httpFactory;
        _logger       = logger;
    }

    private async Task<string?> ResolveApiKeyAsync()
    {
        var userCode = _httpCtx.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userCode))
        {
            var profile = await _intelligence.GetProfileAsync(userCode);
            if (!string.IsNullOrWhiteSpace(profile.ComposioApiKey))
                return profile.ComposioApiKey;
        }
        return _composio.IsConfigured ? _composio.ApiKey : null;
    }

    public string Name => "search_connected_apps";

    public string Description =>
        "Searches data across Composio-connected third-party apps such as QuickBooks Online " +
        "(customers, invoices, bills, payments), OneDrive (files, documents), and any other " +
        "authenticated integrations. Use this when the user asks about data that might live in " +
        "their accounting software, cloud storage, or other integrated platforms. " +
        "Also use this to list which apps are currently connected.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "query":  { "type": "string", "description": "Search term to look for across connected apps. Omit to just list connected apps." },
            "apps":   {
                "type": "array",
                "items": { "type": "string", "enum": ["quickbooks", "onedrive", "all"] },
                "description": "Which apps to search. Defaults to 'all'."
            },
            "entity": { "type": "string", "enum": ["invoices", "customers", "bills", "payments", "files", "all"],
                        "description": "Entity type to filter within an app. Default 'all'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var apiKey = await ResolveApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiToolResult(
                """{"error":"Composio is not configured","hint":"Add your Composio API key in My Intelligence settings, or ask an admin to add a platform key to appsettings.json"}""",
                new AiNoticeSpec(
                    "Composio Not Configured",
                    "Add your personal Composio API key in My Intelligence → Connected Apps, or ask your admin to configure a platform key.",
                    "warning"));
        }

        var query  = args.TryGetProperty("query",  out var qEl) ? qEl.GetString() ?? "" : "";
        var entity = args.TryGetProperty("entity", out var eEl) ? eEl.GetString() ?? "all" : "all";
        var apps   = args.TryGetProperty("apps", out var aEl) && aEl.ValueKind == JsonValueKind.Array
            ? aEl.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
            : new List<string> { "all" };
        var searchAll = apps.Contains("all") || apps.Count == 0;

        var rows = new List<string[]>();
        var connected = new List<string>();

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            // Fetch all connected accounts from Composio
            var accountsResp = await client.GetAsync(
                "https://backend.composio.dev/api/v1/connectedAccounts?pageSize=50", ct);

            if (accountsResp.IsSuccessStatusCode)
            {
                var accountsJson = await accountsResp.Content.ReadAsStringAsync(ct);
                using var accountsDoc = JsonDocument.Parse(accountsJson);

                var items = accountsDoc.RootElement.TryGetProperty("items", out var it)
                    ? it
                    : accountsDoc.RootElement;

                if (items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var acct in items.EnumerateArray())
                    {
                        var appName   = TryStr(acct, "appName") ?? TryStr(acct, "app") ?? "unknown";
                        var status    = TryStr(acct, "status") ?? "unknown";
                        var connId    = TryStr(acct, "id") ?? "";
                        var displayName = TryStr(acct, "displayName") ?? TryStr(acct, "clientUniqueUserId") ?? connId;

                        connected.Add(appName);

                        var appLower = appName.ToLowerInvariant();
                        if (!searchAll && !apps.Any(a => appLower.Contains(a))) continue;

                        // If a search query was provided, fetch entity data from the app
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            var entityRows = await FetchEntityDataAsync(
                                client, connId, appLower, query, entity, ct);
                            rows.AddRange(entityRows);
                        }
                        else
                        {
                            // No query — just list connected accounts
                            rows.Add(new[]
                            {
                                appName,
                                "Connection",
                                displayName,
                                connId[..Math.Min(connId.Length, 16)],
                                status
                            });
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Composio accounts API returned {Status}", accountsResp.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Composio search failed for query={Query}", query);
        }

        if (connected.Count == 0 && rows.Count == 0)
        {
            return new AiToolResult(
                """{"connected":[],"message":"No apps are connected via Composio yet."}""",
                new AiNoticeSpec("No Connected Apps",
                    "No third-party apps are connected via Composio. Connect QuickBooks, OneDrive, or other apps in the Integrations settings.",
                    "info"));
        }

        if (rows.Count == 0)
        {
            var noResultMsg = string.IsNullOrWhiteSpace(query)
                ? "No connected apps found matching the filter."
                : $"No results found for \"{query}\" in connected apps: {string.Join(", ", connected.Distinct())}.";

            return new AiToolResult(
                JsonSerializer.Serialize(new { query, connected = connected.Distinct(), found = 0 }),
                new AiNoticeSpec("Connected Apps Search", noResultMsg, "info"));
        }

        var columns = new[] { "App", "Type", "Name / Title", "ID / Reference", "Status / Amount" };
        var title = string.IsNullOrWhiteSpace(query)
            ? $"Connected Apps ({rows.Count} account{(rows.Count == 1 ? "" : "s")})"
            : $"Connected Apps: \"{query}\" — {rows.Count} result{(rows.Count == 1 ? "" : "s")}";

        return new AiToolResult(
            JsonSerializer.Serialize(new { query, connected = connected.Distinct(), found = rows.Count }),
            new AiTableSpec(title, columns, rows.ToArray()));
    }

    private async Task<List<string[]>> FetchEntityDataAsync(
        HttpClient client, string connectionId, string appName,
        string query, string entity, CancellationToken ct)
    {
        var results = new List<string[]>();

        try
        {
            // Use Composio's execute-action endpoint to query entity data.
            // Action names follow the pattern: APPNAME_LIST_ENTITY or APPNAME_GET_ENTITY
            var actions = ResolveActions(appName, entity);

            foreach (var action in actions)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        connectedAccountId = connectionId,
                        input = new { query = query, searchTerm = query, q = query, limit = 10, maxResults = 10 }
                    });

                    var resp = await client.PostAsync(
                        $"https://backend.composio.dev/api/v1/actions/{action}/execute",
                        new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
                        ct);

                    if (!resp.IsSuccessStatusCode) continue;

                    var json = await resp.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(json);

                    var dataEl = doc.RootElement.TryGetProperty("data", out var d) ? d
                               : doc.RootElement.TryGetProperty("result", out var r) ? r
                               : doc.RootElement;

                    ExtractRows(dataEl, appName, action, query, results);

                    if (results.Count > 0) break; // found data, stop trying fallback actions
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Action {Action} failed for {App}", action, appName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FetchEntityData failed for {App}", appName);
        }

        return results;
    }

    private static IEnumerable<string> ResolveActions(string appName, string entity)
    {
        var prefix = appName.ToUpperInvariant().Replace("-", "_");
        return entity switch
        {
            "invoices"  => new[] { $"{prefix}_LIST_INVOICES",  $"{prefix}_QUERY_INVOICES",  $"{prefix}_FIND_INVOICE" },
            "customers" => new[] { $"{prefix}_LIST_CUSTOMERS", $"{prefix}_QUERY_CUSTOMERS", $"{prefix}_FIND_CUSTOMER" },
            "bills"     => new[] { $"{prefix}_LIST_BILLS",     $"{prefix}_QUERY_BILLS" },
            "payments"  => new[] { $"{prefix}_LIST_PAYMENTS",  $"{prefix}_QUERY_PAYMENTS" },
            "files"     => new[] { $"{prefix}_SEARCH_FILES",   $"{prefix}_LIST_FILES",      $"{prefix}_FIND_FILES" },
            _           => new[] { $"{prefix}_SEARCH",         $"{prefix}_LIST_ITEMS",      $"{prefix}_QUERY" }
        };
    }

    private static void ExtractRows(
        JsonElement el, string appName, string action, string query, List<string[]> rows)
    {
        // Walk the response looking for array-like data
        var arr = el.ValueKind == JsonValueKind.Array ? el
                : el.TryGetProperty("QueryResponse", out var qr) && qr.ValueKind == JsonValueKind.Object
                    ? FindFirstArray(qr) ?? el
                : FindFirstArray(el) ?? el;

        if (arr.ValueKind != JsonValueKind.Array)
        {
            // Single object — wrap
            var row = ObjectToRow(el, appName, action);
            if (row != null) rows.Add(row);
            return;
        }

        foreach (var item in arr.EnumerateArray().Take(10))
        {
            var row = ObjectToRow(item, appName, action);
            if (row != null) rows.Add(row);
        }
    }

    private static JsonElement? FindFirstArray(JsonElement el)
    {
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                return prop.Value;
        }
        return null;
    }

    private static string[]? ObjectToRow(JsonElement item, string appName, string action)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;

        var name = TryStr(item, "Name") ?? TryStr(item, "DisplayName") ?? TryStr(item, "Title")
                ?? TryStr(item, "FullyQualifiedName") ?? TryStr(item, "name") ?? TryStr(item, "title") ?? "";
        if (string.IsNullOrWhiteSpace(name)) return null;

        var id = TryStr(item, "Id") ?? TryStr(item, "id") ?? TryStr(item, "DocNumber") ?? "";
        var statusOrAmount = TryStr(item, "Balance") ?? TryStr(item, "TotalAmt") ?? TryStr(item, "Status")
                          ?? TryStr(item, "status") ?? TryStr(item, "size") ?? "";

        var entityType = action.Replace(appName.ToUpperInvariant() + "_", "")
                               .Replace("LIST_", "").Replace("QUERY_", "").Replace("FIND_", "")
                               .ToLower();

        return new[] { appName, entityType, name, id, statusOrAmount };
    }

    private static string? TryStr(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString()
             : v.ValueKind == JsonValueKind.Null   ? null
             : v.ToString();
    }
}
