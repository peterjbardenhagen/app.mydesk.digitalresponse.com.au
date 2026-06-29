using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Composio integration for QuickBooks Online, bank feeds, and Frollo API connections.
/// </summary>
public class ComposioIntegrationService
{
    private readonly ILogger<ComposioIntegrationService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ComposioOptions _options;

    public ComposioIntegrationService(
        IHttpClientFactory httpFactory,
        ILogger<ComposioIntegrationService> logger,
        IOptions<ComposioOptions> options)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_options.ApiKey);
    public string ApiKey => _options.ApiKey;

    /// <summary>
    /// Initiates QuickBooks OAuth flow - returns redirect URL for user authentication
    /// </summary>
    public string GetQuickBooksAuthUrl(string platformSlug, string redirectUri)
    {
        return $"https://backend.composio.dev/api/v1/auth/qb/start?platform={platformSlug}&redirect={redirectUri}";
    }

    /// <summary>
    /// Fetches QuickBooks entities (invoices, customers, bills) for reconciliation
    /// </summary>
    public async Task<QuickBooksSyncResult> SyncQuickBooksEntitiesAsync(string connectionId, string entityType = "all")
    {
        if (!IsConfigured) return new QuickBooksSyncResult { Error = "Composio not configured" };

        var client = _httpFactory.CreateClient("Composio");
        var result = new QuickBooksSyncResult();

        try
        {
            var entities = entityType == "all" 
                ? new[] { "invoices", "bills", "customers", "payments" } 
                : new[] { entityType };

            foreach (var entity in entities)
            {
                var response = await client.PostAsync(
                    $"https://backend.composio.dev/api/v1/connections/{connectionId}/qbo/sync/{entity}",
                    new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Synced {Entity} for connection {Connection}", entity, connectionId);
                }
            }
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync QuickBooks entities");
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Sets up Frollo bank statement automation for a client platform
    /// </summary>
    public async Task<FrolloSetupResult> SetupFrolloIntegrationAsync(ClientPlatform platform, ClientPlatformSettings settings)
    {
        if (!platform.EnableFrolloIntegration)
            return new FrolloSetupResult { Error = "Frollo integration not enabled for platform" };

        var client = _httpFactory.CreateClient("Frollo");
        var result = new FrolloSetupResult();

        try
        {
            // Create bank account record for automated statement fetching
            var payload = new
            {
                institutionId = settings.FrolloInstitutionId,
                accountId = settings.FrolloAccountId,
                platformId = platform.Id,
                autoFetch = true,
                syncFrequencyHours = 24
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync("https://api.frollo.com.au/v2/accounts/setup", 
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                result.Success = true;
                result.AuthUrl = "https://api.frollo.com.au/oauth/authorize";
            }
            else
            {
                result.Error = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup Frollo integration for platform {Platform}", platform.Name);
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Fetches bank statements from Frollo API
    /// </summary>
    public async Task<List<FrolloStatement>> GetFrolloStatementsAsync(string accessToken, string accountId)
    {
        var client = _httpFactory.CreateClient("Frollo");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.GetAsync($"https://api.frollo.com.au/v2/accounts/{accountId}/statements");
        if (!response.IsSuccessStatusCode)
            return new List<FrolloStatement>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<FrolloStatement>>(json) ?? new List<FrolloStatement>();
    }
}

public class ComposioOptions
{
    public string ApiKey { get; set; } = "";
    public string ApiUrl { get; set; } = "https://backend.composio.dev/api/v1";
}

public class QuickBooksSyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime LastSync { get; set; }
}

public class FrolloSetupResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AuthUrl { get; set; }
}

public class FrolloStatement
{
    public string StatementId { get; set; } = "";
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<FrolloTransaction> Transactions { get; set; } = new();
}

public class FrolloTransaction
{
    public string TransactionId { get; set; } = "";
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Direction { get; set; } = "debit"; // debit or credit
    public string Category { get; set; } = "";
}