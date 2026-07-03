using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDesk.Shared.Services;
using MyDesk.Web.AI;
using MyDesk.Web.Api;
using MyDesk.Web.Services;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Model Context Protocol (MCP) server — JSON-RPC 2.0 over HTTP.
/// Exposes all of MyDesk's AI tools to external agents (Claude Desktop,
/// ChatGPT Custom Actions, Microsoft Copilot, Gemini, n8n, Zapier, etc.).
///
/// Transport:  POST /api/mcp  (request/response; no SSE streaming required for tool calls)
///             GET  /api/mcp/openapi.json  (OpenAPI 3.0 spec for ChatGPT / Gemini)
///             GET  /api/mcp/copilot-plugin.json  (Microsoft Copilot plugin manifest)
///             GET  /api/mcp/claude-config.json   (Claude Desktop config snippet)
///
/// Auth:  Authorization: Bearer mdk_xxx  (Personal Access Token)
///        The token carries user + tenant identity so every tool call is automatically
///        scoped to that user's tenant and respects their role.
///
/// Tool availability is dynamic: the set returned by tools/list reflects the calling
/// tenant's active modules and integrations (accounting, legal, CRM, etc.).
/// </summary>
[ApiController]
[Route("api/mcp")]
[Authorize(AuthenticationSchemes = PersonalAccessTokenAuthHandler.SchemeName)]
public sealed class McpServerController : ControllerBase
{
    private readonly IEnumerable<IAiTool>           _tools;
    private readonly AskAiAgentService              _agent;
    private readonly PlatformSettingsService        _platformSettings;
    private readonly ICurrentTenantAccessor         _tenant;
    private readonly ILogger<McpServerController>   _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    public McpServerController(
        IEnumerable<IAiTool>        tools,
        AskAiAgentService           agent,
        PlatformSettingsService     platformSettings,
        ICurrentTenantAccessor      tenant,
        ILogger<McpServerController> logger)
    {
        _tools            = tools;
        _agent            = agent;
        _platformSettings = platformSettings;
        _tenant           = tenant;
        _logger           = logger;
    }

    // ── JSON-RPC 2.0 dispatcher ───────────────────────────────────────────────

    [HttpPost]
    [AllowAnonymous] // PAT validation happens via the auth scheme; this allows the OPTIONS pre-flight
    [Authorize(AuthenticationSchemes = PersonalAccessTokenAuthHandler.SchemeName)]
    public async Task<IActionResult> HandleAsync(CancellationToken ct)
    {
        McpRequest? rpc;
        try
        {
            rpc = await JsonSerializer.DeserializeAsync<McpRequest>(
                Request.Body, _json, ct);
        }
        catch
        {
            return Ok(McpError(-32700, "Parse error", null));
        }

        if (rpc is null)
            return Ok(McpError(-32600, "Invalid Request", null));

        _logger.LogDebug("MCP {Method} from tenant={Tenant}", rpc.Method, _tenant.TenantId);

        return rpc.Method switch
        {
            "initialize"          => Ok(await HandleInitializeAsync(rpc)),
            "notifications/initialized" => Ok(McpOk(rpc.Id, new { })),
            "tools/list"          => Ok(await HandleToolsListAsync(rpc)),
            "tools/call"          => Ok(await HandleToolsCallAsync(rpc, ct)),
            "resources/list"      => Ok(McpOk(rpc.Id, new { resources = Array.Empty<object>() })),
            "prompts/list"        => Ok(McpOk(rpc.Id, new { prompts = Array.Empty<object>() })),
            _                     => Ok(McpError(-32601, $"Method not found: {rpc.Method}", rpc.Id)),
        };
    }

    // ── initialize ────────────────────────────────────────────────────────────

    private Task<object> HandleInitializeAsync(McpRequest rpc)
    {
        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities    = new
            {
                tools     = new { listChanged = false },
                resources = new { listChanged = false },
                prompts   = new { listChanged = false },
            },
            serverInfo = new
            {
                name    = "MyDesk",
                version = "2.0",
            },
            instructions = $"""
                You are connected to MyDesk — the AI-powered business management platform.
                Tenant: {_tenant.TenantName ?? "MyDesk"}.
                User: {User.Identity?.Name ?? "unknown"}.

                Use the available tools to look up real-time data: quotes, invoices,
                pipeline, cash flow, customers, and more. Always scope your answers to
                this tenant's data. Use ask_desky for open-ended business questions.
                """,
        };
        return Task.FromResult(McpOk(rpc.Id, result));
    }

    // ── tools/list ────────────────────────────────────────────────────────────

    private async Task<object> HandleToolsListAsync(McpRequest rpc)
    {
        var availableTools = await GetAvailableToolsAsync();
        var list = availableTools.Select(t => new
        {
            name        = t.Name,
            description = t.Description,
            inputSchema = t.ParametersSchema,
        }).ToList();

        // Always include the ask_desky meta-tool
        list.Add(new
        {
            name        = "ask_desky",
            description = "Ask Desky (MyDesk's AI) an open-ended business question. " +
                          "Desky has access to your business data and can answer questions about " +
                          "strategy, pipeline, revenue, OKRs, clients, and operational decisions. " +
                          "Use this when no specific tool matches the query.",
            inputSchema = JsonSerializer.SerializeToElement(new
            {
                type       = "object",
                required   = new[] { "question" },
                properties = new
                {
                    question = new { type = "string", description = "The question to ask Desky." },
                    history  = new
                    {
                        type  = "array",
                        description = "Optional prior conversation turns for multi-turn context.",
                        items = new
                        {
                            type       = "object",
                            properties = new
                            {
                                role    = new { type = "string", @enum = new[] { "user", "assistant" } },
                                content = new { type = "string" },
                            }
                        }
                    }
                }
            }),
        });

        return McpOk(rpc.Id, new { tools = list });
    }

    // ── tools/call ────────────────────────────────────────────────────────────

    private async Task<object> HandleToolsCallAsync(McpRequest rpc, CancellationToken ct)
    {
        var paramsEl = rpc.Params ?? default;
        string? toolName = null;
        JsonElement argsEl = default;

        try
        {
            toolName = paramsEl.GetProperty("name").GetString();
            argsEl   = paramsEl.TryGetProperty("arguments", out var a) ? a : JsonDocument.Parse("{}").RootElement;
        }
        catch
        {
            return McpError(-32602, "Invalid params: 'name' required", rpc.Id);
        }

        if (string.IsNullOrWhiteSpace(toolName))
            return McpError(-32602, "Tool name is required", rpc.Id);

        // ── ask_desky meta-tool ────────────────────────────────────────────────
        if (toolName == "ask_desky")
        {
            var question = argsEl.TryGetProperty("question", out var q) ? q.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(question))
                return McpToolError(rpc.Id, "question is required");

            // Build history from optional prior turns
            var history = new List<object>();
            if (argsEl.TryGetProperty("history", out var hEl) && hEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var turn in hEl.EnumerateArray())
                {
                    var role    = turn.TryGetProperty("role",    out var r) ? r.GetString() : null;
                    var content = turn.TryGetProperty("content", out var c) ? c.GetString() : null;
                    if (!string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(content))
                        history.Add(new Dictionary<string, object?> { ["role"] = role, ["content"] = content });
                }
            }

            try
            {
                var reply = await _agent.AskAsync(question, systemPrompt: DeskySystemPrompt(), ct: ct);
                return McpOk(rpc.Id, new
                {
                    content = new[] { new { type = "text", text = reply.Text } },
                    isError = false,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ask_desky tool error");
                return McpToolError(rpc.Id, "Desky encountered an error processing your question.");
            }
        }

        // ── Regular registered tools ───────────────────────────────────────────
        var availableTools = await GetAvailableToolsAsync();
        var tool = availableTools.FirstOrDefault(t =>
            string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));

        if (tool is null)
        {
            return McpError(-32602,
                $"Tool '{toolName}' is not available for this tenant. " +
                $"Available tools: {string.Join(", ", availableTools.Select(t => t.Name))}, ask_desky",
                rpc.Id);
        }

        try
        {
            var result = await tool.ExecuteAsync(argsEl, ct);
            return McpOk(rpc.Id, new
            {
                content = new[] { new { type = "text", text = result.ContentJson } },
                isError = false,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP tool '{Tool}' error for tenant {Tenant}", toolName, _tenant.TenantId);
            return McpToolError(rpc.Id, $"Tool '{toolName}' encountered an error: {ex.Message}");
        }
    }

    // ── OpenAPI spec (ChatGPT / Gemini actions) ───────────────────────────────

    [HttpGet("openapi.json")]
    [AllowAnonymous]
    public async Task<IActionResult> OpenApiSpecAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var tools   = _tools.ToList();

        var paths = new Dictionary<string, object>();

        // One path per tool
        foreach (var tool in tools)
        {
            paths[$"/api/tools/{tool.Name}"] = new
            {
                post = new
                {
                    operationId = tool.Name,
                    summary     = tool.Description,
                    tags        = new[] { "MyDesk Tools" },
                    security    = new[] { new Dictionary<string, object[]> { ["BearerAuth"] = [] } },
                    requestBody = new
                    {
                        required = true,
                        content  = new
                        {
                            application_json = new { schema = tool.ParametersSchema }
                        }
                    },
                    responses = new
                    {
                        _200 = new { description = "Tool result", content = new { application_json = new { schema = new { type = "object", properties = new { result = new { type = "string" } } } } } }
                    }
                }
            };
        }

        // ask_desky always included
        paths["/api/chat/desky"] = new
        {
            post = new
            {
                operationId = "ask_desky",
                summary     = "Ask Desky — MyDesk AI for open-ended business questions",
                tags        = new[] { "Desky AI" },
                security    = new[] { new Dictionary<string, object[]> { ["BearerAuth"] = [] } },
                requestBody = new
                {
                    required = true,
                    content  = new { application_json = new { schema = new { type = "object", required = new[] { "message" }, properties = new { message = new { type = "string" }, brand = new { type = "string" }, history = new { type = "array" } } } } }
                },
                responses = new
                {
                    _200 = new { description = "Desky reply", content = new { application_json = new { schema = new { type = "object", properties = new { reply = new { type = "string" } } } } } }
                }
            }
        };

        var spec = new
        {
            openapi = "3.1.0",
            info    = new
            {
                title       = "MyDesk API",
                description = "AI-powered business management platform. Provides tools for quotes, invoices, pipeline, cash flow, customers, and more.",
                version     = "2.0",
            },
            servers  = new[] { new { url = baseUrl, description = "MyDesk" } },
            security = new[] { new Dictionary<string, object[]> { ["BearerAuth"] = [] } },
            components = new
            {
                securitySchemes = new
                {
                    BearerAuth = new
                    {
                        type        = "http",
                        scheme      = "bearer",
                        description = "Personal Access Token. Generate one at /ai-agents in your MyDesk workspace.",
                    }
                }
            },
            paths,
        };

        return new JsonResult(spec, _json);
    }

    // ── Microsoft Copilot plugin manifest ─────────────────────────────────────

    [HttpGet("copilot-plugin.json")]
    [AllowAnonymous]
    public IActionResult CopilotPluginManifest()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var manifest = new
        {
            schema_version = "v2.1",
            name_for_human = "MyDesk",
            name_for_model = "mydesk",
            description_for_human  = "AI-powered business management: quotes, invoices, pipeline, cash flow, and Desky AI.",
            description_for_model  = "MyDesk tools for looking up business data. Use get_quotes for quote status, get_invoices for invoice status, get_pipeline for pipeline summary, get_cashflow for cash flow forecasts, get_customers for top customers, and ask_desky for open-ended business questions.",
            auth = new
            {
                type = "user_http",
                authorization_type = "bearer",
            },
            api = new
            {
                type = "openapi",
                url  = $"{baseUrl}/api/mcp/openapi.json",
            },
            logo_url         = $"{baseUrl}/images/desky-logo.png",
            contact_email    = "support@digitalresponse.com.au",
            legal_info_url   = $"{baseUrl}/legal",
        };
        return new JsonResult(manifest, _json);
    }

    // ── Claude Desktop config snippet ─────────────────────────────────────────

    [HttpGet("claude-config.json")]
    [AllowAnonymous]
    public IActionResult ClaudeConfig()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var config = new
        {
            mcpServers = new
            {
                mydesk = new
                {
                    url     = $"{baseUrl}/api/mcp",
                    headers = new { Authorization = "Bearer YOUR_MDK_TOKEN_HERE" },
                }
            }
        };
        return new JsonResult(config, _json);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the tools available to the current tenant, filtered by their active
    /// modules and platform integrations.
    /// Legal tools only appear for tenants with the legal module enabled.
    /// Accounting tools reflect the tenant's connected accounting platform.
    /// </summary>
    private Task<List<IAiTool>> GetAvailableToolsAsync()
    {
        var settings = _platformSettings.Current;
        var all      = _tools.ToList();

        var available = all.Where(t =>
        {
            // Legal tools — only for legal module tenants
            if (t.Name.StartsWith("radix_") || t.Name.StartsWith("legal_"))
                return settings?.EnableLegalModule == true;

            // Accounting tools — only when an accounting integration is connected
            if (t.Name.StartsWith("myob_") || t.Name.StartsWith("xero_") || t.Name.StartsWith("qbo_"))
                return settings?.IntegrationSettings?.AccountingProvider is not null;

            return true; // all standard tools are always available
        }).ToList();

        return Task.FromResult(available);
    }

    private string DeskySystemPrompt() => $"""
        You are Desky — the AI embedded in MyDesk for {_tenant.TenantName ?? "this business"}.
        You are advising {User.Identity?.Name ?? "the user"}.

        Character: Virtual MBA. Risk-averse, commercially sharp, never leaves money on the table.
        Runs 24/7 background simulations to keep OKRs on track. Australian English. Concise.

        You have access to tools. Use them to look up real data. Replies: ≤120 words unless
        the user asks for detail. Plain text — no markdown headers.
        """;

    // ── JSON-RPC helpers ──────────────────────────────────────────────────────

    private static object McpOk(JsonElement? id, object result) =>
        new { jsonrpc = "2.0", id, result };

    private static object McpError(int code, string message, JsonElement? id) =>
        new { jsonrpc = "2.0", id, error = new { code, message } };

    private static object McpToolError(JsonElement? id, string message) =>
        new { jsonrpc = "2.0", id, result = new { content = new[] { new { type = "text", text = message } }, isError = true } };
}

// ── Request model ─────────────────────────────────────────────────────────────

public sealed class McpRequest
{
    [JsonPropertyName("jsonrpc")] public string      Jsonrpc { get; set; } = "2.0";
    [JsonPropertyName("id")]      public JsonElement? Id     { get; set; }
    [JsonPropertyName("method")]  public string      Method  { get; set; } = "";
    [JsonPropertyName("params")]  public JsonElement? Params { get; set; }
}
