using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services.Legal;

/// <summary>
/// Client for the Radix legal practice management REST API.
/// Timesheets use 6-minute billing units (1 unit = 6 min).
/// </summary>
public class RadixService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PlatformSettingsService _settings;
    private readonly ILogger<RadixService> _logger;

    public RadixService(
        IHttpClientFactory httpFactory,
        PlatformSettingsService settings,
        ILogger<RadixService> logger)
    {
        _httpFactory = httpFactory;
        _settings = settings;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Current.RadixApiUrl) &&
        !string.IsNullOrWhiteSpace(_settings.Current.RadixApiKey);

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_settings.Current.RadixApiUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.Current.RadixApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public async Task<List<RadixTimesheet>> GetTimesheetsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? feeEarnerCode = null,
        CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        var query = new List<string>();
        if (from.HasValue)  query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)    query.Add($"to={to.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(feeEarnerCode)) query.Add($"feeEarner={Uri.EscapeDataString(feeEarnerCode)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        try
        {
            using var client = CreateClient();
            var json = await client.GetStringAsync($"api/timesheets{qs}", ct);
            var result = JsonSerializer.Deserialize<List<RadixTimesheet>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radix GetTimesheets failed");
            return new();
        }
    }

    public async Task<List<RadixMatter>> GetMattersAsync(string? search = null, CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        var qs = string.IsNullOrWhiteSpace(search) ? "" : $"?search={Uri.EscapeDataString(search)}";
        try
        {
            using var client = CreateClient();
            var json = await client.GetStringAsync($"api/matters{qs}", ct);
            var result = JsonSerializer.Deserialize<List<RadixMatter>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radix GetMatters failed");
            return new();
        }
    }

    public async Task<RadixTimesheet?> CreateTimesheetAsync(RadixTimesheetCreate entry, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;
        try
        {
            using var client = CreateClient();
            var body = JsonSerializer.Serialize(entry);
            var resp = await client.PostAsync("api/timesheets",
                new StringContent(body, Encoding.UTF8, "application/json"), ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<RadixTimesheet>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radix CreateTimesheet failed");
            return null;
        }
    }
}

public record RadixTimesheet
{
    public string Id { get; init; } = "";
    public string FeeEarnerCode { get; init; } = "";
    public string FeeEarnerName { get; init; } = "";
    public string MatterNumber { get; init; } = "";
    public string MatterName { get; init; } = "";
    public DateTime Date { get; init; }
    public int Units { get; init; }
    public string ActivityCode { get; init; } = "";
    public string Narrative { get; init; } = "";
    public decimal Rate { get; init; }
    public decimal WipAmount { get; init; }
    public string Status { get; init; } = "Draft";
}

public record RadixTimesheetCreate
{
    public string FeeEarnerCode { get; init; } = "";
    public string MatterNumber { get; init; } = "";
    public DateTime Date { get; init; }
    public int Units { get; init; }
    public string ActivityCode { get; init; } = "";
    public string Narrative { get; init; } = "";
}

public record RadixMatter
{
    public string MatterNumber { get; init; } = "";
    public string MatterName { get; init; } = "";
    public string ClientName { get; init; } = "";
    public string ResponsibleLawyer { get; init; } = "";
    public string Status { get; init; } = "";
}
