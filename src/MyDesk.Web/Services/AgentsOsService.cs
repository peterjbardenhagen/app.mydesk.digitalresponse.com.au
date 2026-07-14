using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyDesk.Shared.Models.AgentsOS;

namespace MyDesk.Web.Services;

public class AgentsOsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AgentsOsService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public AgentsOsService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AgentsOsService> logger)
    {
        _http = httpClientFactory.CreateClient("AgentsOS");
        _config = config;
        _logger = logger;
    }

    private string? BaseUrl => _config["AgentsOS:BaseUrl"];

    public async Task<AgentsOsResponse<PdoResponse>?> CreateProjectAsync(string title, string brief)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var payload = JsonSerializer.Serialize(new { title, brief });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{baseUrl}/projects", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var raw = JsonSerializer.Deserialize<JsonElement>(json);
            return new AgentsOsResponse<PdoResponse>
            {
                Status = raw.GetProperty("status").GetString() ?? "unknown",
                ProjectId = raw.GetProperty("project_id").GetString(),
                Data = JsonSerializer.Deserialize<PdoResponse>(raw.GetProperty("pdo").GetRawText(), JsonOpts),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS CreateProject failed: {Title}", title);
            return null;
        }
    }

    public async Task<OrchestratorResult?> PlanProjectAsync(string projectId, string? brief = null)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var body = brief is not null ? JsonSerializer.Serialize(new { brief }) : "{}";
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{baseUrl}/projects/{projectId}/plan", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OrchestratorResult>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS PlanProject failed: {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<OrchestratorResult?> ExecuteProjectAsync(string projectId, string? brief = null)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var body = brief is not null ? JsonSerializer.Serialize(new { brief }) : "{}";
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{baseUrl}/projects/{projectId}/execute", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OrchestratorResult>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS ExecuteProject failed: {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<List<PdoResponse>> ListProjectsAsync()
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new();
        try
        {
            var response = await _http.GetAsync($"{baseUrl}/projects");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var raw = JsonSerializer.Deserialize<JsonElement>(json);
            if (raw.TryGetProperty("projects", out var projects))
            {
                return JsonSerializer.Deserialize<List<PdoResponse>>(projects.GetRawText(), JsonOpts) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS ListProjects failed");
            return new();
        }
    }

    public async Task<PdoResponse?> GetProjectAsync(string projectId)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var response = await _http.GetAsync($"{baseUrl}/projects/{projectId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var raw = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Deserialize<PdoResponse>(raw.GetProperty("pdo").GetRawText(), JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS GetProject failed: {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<DagResponse?> GetProjectDagAsync(string projectId)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var response = await _http.GetAsync($"{baseUrl}/projects/{projectId}/dag");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DagResponse>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS GetProjectDag failed: {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<LedgerResponse?> GetProjectLedgerAsync(string projectId, string source = "memory", int limit = 100)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var response = await _http.GetAsync($"{baseUrl}/projects/{projectId}/ledger?source={source}&limit={limit}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LedgerResponse>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS GetProjectLedger failed: {ProjectId}", projectId);
            return null;
        }
    }

    /// Human approval: approve or reject a gated DAG task
    public async Task<AgentsOsResponse<object>?> ApproveTaskAsync(string projectId, string taskId, bool approved, string? notes = null)
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var payload = JsonSerializer.Serialize(new { project_id = projectId, task_id = taskId, approved, notes });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{baseUrl}/projects/{projectId}/approve", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AgentsOsResponse<object>>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentsOS ApproveTask failed: {ProjectId}/{TaskId}", projectId, taskId);
            return null;
        }
    }

    public async Task<bool> IsReachableAsync()
    {
        var baseUrl = BaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return false;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await _http.GetAsync($"{baseUrl}/projects?limit=1", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
