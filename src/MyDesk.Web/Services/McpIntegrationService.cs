using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyDesk.Web.Services;

/// <summary>
/// Integration service for calling both MyDeskMCP and MYOBMCP servers.
/// Provides unified access to Techlight data and MYOB accounting data for the AI assistant.
/// </summary>
public class McpIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<McpIntegrationService> _logger;

    public McpIntegrationService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<McpIntegrationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MyDeskMCP Methods (Techlight Operations)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<object?> CallMyDeskToolAsync(string toolName, Dictionary<string, object> arguments, string userCode)
    {
        var baseUrl = _config["McpServer:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogWarning("MyDeskMCP BaseUrl not configured");
            return null;
        }

        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments
                },
                id = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authentication headers
            content.Headers.Add("X-User-Code", userCode);
            content.Headers.Add("X-API-Key", _config["Api:Key"] ?? "");

            var response = await _httpClient.PostAsync($"{baseUrl}/mcp/v1/tools/call", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<McpToolResponse>(responseJson);
            
            if (result?.Error != null)
            {
                _logger.LogWarning("MyDeskMCP tool error: {Message}", result.Error.Message);
                return null;
            }

            return result?.Result?.Content?.FirstOrDefault()?.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MyDeskMCP tool {Tool}", toolName);
            return null;
        }
    }

    public async Task<string> GetMyDeskContextAsync(string userCode)
    {
        try
        {
            // Get user info
            var userInfo = await CallMyDeskToolAsync("get_user_info", new(), userCode);
            
            // Get recent quotes
            var quotes = await CallMyDeskToolAsync("list_quotes", new() 
            { 
                ["limit"] = 5,
                ["date_from"] = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd")
            }, userCode);

            // Get recent invoices
            var invoices = await CallMyDeskToolAsync("list_invoices", new() 
            { 
                ["limit"] = 5,
                ["date_from"] = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd")
            }, userCode);

            // Get outstanding purchase orders
            var pos = await CallMyDeskToolAsync("list_purchase_orders", new() 
            { 
                ["limit"] = 5,
                ["status"] = "Pending"
            }, userCode);

            return $@"Techlight MyDesk Current Context:
User: {userCode}

Recent Quotes (last 30 days): {SerializeForPrompt(quotes)}

Recent Invoices (last 30 days): {SerializeForPrompt(invoices)}

Pending Purchase Orders: {SerializeForPrompt(pos)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MyDesk context");
            return "Techlight context unavailable.";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MYOBMCP Methods (Accounting Data)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<object?> CallMYOBToolAsync(string toolName, Dictionary<string, object> arguments)
    {
        var baseUrl = _config["MyobMcpServer:BaseUrl"];
        var apiKey = _config["MyobMcpServer:ApiKey"];
        
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogWarning("MYOBMCP BaseUrl not configured");
            return null;
        }

        try
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments
                },
                id = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authentication header
            if (!string.IsNullOrEmpty(apiKey))
            {
                content.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.PostAsync($"{baseUrl}/mcp/v1/tools/call", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<McpToolResponse>(responseJson);
            
            if (result?.Error != null)
            {
                _logger.LogWarning("MYOBMCP tool error: {Message}", result.Error.Message);
                return null;
            }

            return result?.Result?.Content?.FirstOrDefault()?.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MYOBMCP tool {Tool}", toolName);
            return null;
        }
    }

    public async Task<string> GetMYOBContextAsync()
    {
        try
        {
            // Get business summary
            var summary = await CallMYOBToolAsync("myob_business_summary", new());
            
            // Get outstanding receivables
            var outstanding = await CallMYOBToolAsync("myob_get_outstanding_invoices", new());
            
            // Get aged receivables
            var aged = await CallMYOBToolAsync("myob_aged_receivables", new());

            // Get bank balance
            var bankBalance = await CallMYOBToolAsync("myob_get_bank_balance", new());

            return $@"MYOB Accounting Context:

Business Summary: {SerializeForPrompt(summary)}

Outstanding Invoices: {SerializeForPrompt(outstanding)}

Aged Receivables: {SerializeForPrompt(aged)}

Bank Balance: {SerializeForPrompt(bankBalance)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MYOB context");
            return "MYOB accounting context unavailable.";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Combined Context for AI
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> GetCombinedContextAsync(string userCode)
    {
        var myDeskTask = GetMyDeskContextAsync(userCode);
        var myobTask = GetMYOBContextAsync();

        await Task.WhenAll(myDeskTask, myobTask);

        return $@"{await myDeskTask}

---

{await myobTask}

---

You have access to the following tools:
- MyDeskMCP tools for Techlight operations (quotes, invoices, purchase orders, contacts)
- MYOBMCP tools for accounting data (customers, invoices, payments, reports, bank balance)

When responding to user queries about business operations, consider both the Techlight (sales/operations) data and MYOB (accounting/financial) data.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Specific Tool Helpers
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<object?> SearchMYOBCustomerAsync(string name)
    {
        return await CallMYOBToolAsync("myob_search_customer", new() { ["name"] = name });
    }

    public async Task<object?> GetMYOBCustomerBalanceAsync(string uid)
    {
        return await CallMYOBToolAsync("myob_get_customer_balance", new() { ["uid"] = uid });
    }

    public async Task<object?> GetMYOBCustomerInvoicesAsync(string customerUid)
    {
        return await CallMYOBToolAsync("myob_list_invoices", new() { ["customer_uid"] = customerUid });
    }

    public async Task<object?> CompareCustomerBalanceAsync(string customerName)
    {
        return await CallMYOBToolAsync("myob_compare_customer_balance", new() { ["customer_name"] = customerName });
    }

    public async Task<object?> GetMYOBProfitLossAsync(DateTime startDate, DateTime endDate)
    {
        return await CallMYOBToolAsync("myob_profit_loss_report", new() 
        { 
            ["start_date"] = startDate.ToString("yyyy-MM-dd"),
            ["end_date"] = endDate.ToString("yyyy-MM-dd")
        });
    }

    public async Task<object?> GetMYOBMonthlyRevenueAsync(int year, int month)
    {
        return await CallMYOBToolAsync("myob_get_monthly_revenue", new() 
        { 
            ["year"] = year,
            ["month"] = month
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────────────────────────────────

    private string SerializeForPrompt(object? obj)
    {
        if (obj == null) return "No data available";
        
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(obj, options);
        }
        catch
        {
            return obj?.ToString() ?? "Error serializing data";
        }
    }
}

// Response Models
public class McpToolResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("result")]
    public McpToolResult? Result { get; set; }
    
    [JsonPropertyName("error")]
    public McpToolError? Error { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpToolContent>? Content { get; set; }
}

public class McpToolContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class McpToolError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
