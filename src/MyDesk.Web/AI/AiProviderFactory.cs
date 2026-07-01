using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;
using MyDesk.Web.AI.Providers;
using MyDesk.Web.Services;

namespace MyDesk.Web.AI;

/// <summary>
/// Resolves the active <see cref="IAiChatProvider"/> for the current tenant.
///
/// Priority order:
///   1. Tenant's own <see cref="AiProviderConfig"/> stored in <see cref="PlatformSettings"/>
///      (if <see cref="AiProviderConfig.IsConfigured"/> is true).
///   2. Fall back to the platform-level Azure AI Foundry config from appsettings.json
///      (the legacy <see cref="AzureAIOptions"/> binding).
///
/// This lets Platform Admins keep a default Azure deployment while individual tenants
/// can plug in their own OpenAI / Gemini / Openrouter / Ollama keys.
/// </summary>
public sealed class AiProviderFactory
{
    private readonly PlatformSettingsService _settings;
    private readonly AzureAIOptions          _azureOpts;
    private readonly IHttpClientFactory      _http;
    private readonly ILoggerFactory          _loggers;

    public AiProviderFactory(
        PlatformSettingsService settings,
        IOptions<AzureAIOptions> azureOpts,
        IHttpClientFactory http,
        ILoggerFactory loggers)
    {
        _settings  = settings;
        _azureOpts = azureOpts.Value;
        _http      = http;
        _loggers   = loggers;
    }

    /// <summary>Returns the configured provider, never null (may be unconfigured).</summary>
    public IAiChatProvider Resolve()
    {
        var tenantCfg = _settings.Current.AiProvider;
        if (tenantCfg?.IsConfigured == true)
        {
            _loggers.CreateLogger<AiProviderFactory>()
                .LogDebug("Using tenant AI provider: {Provider} / {Model}",
                    tenantCfg.Provider, tenantCfg.ChatModel);
            return new OpenAiCompatibleProvider(tenantCfg, _http,
                _loggers.CreateLogger<OpenAiCompatibleProvider>());
        }

        // Fall back to the Azure AI Foundry config from appsettings.json
        if (_azureOpts.IsConfigured)
        {
            var azureCfg = new AiProviderConfig
            {
                Provider        = AiProviderKind.AzureAI,
                ApiKey          = _azureOpts.OpenAIApiKey,
                AzureEndpoint   = _azureOpts.OpenAIEndpoint,
                AzureApiVersion = _azureOpts.OpenAIApiVersion,
                AzureDeployment = _azureOpts.OpenAIModel,
                ChatModel       = _azureOpts.OpenAIModel,
            };
            return new OpenAiCompatibleProvider(azureCfg, _http,
                _loggers.CreateLogger<OpenAiCompatibleProvider>());
        }

        // Neither configured — return unconfigured provider so callers get a friendly message.
        return new OpenAiCompatibleProvider(new AiProviderConfig(), _http,
            _loggers.CreateLogger<OpenAiCompatibleProvider>());
    }

    /// <summary>True when either the tenant config or the platform Azure config is ready.</summary>
    public bool IsConfigured =>
        _settings.Current.AiProvider?.IsConfigured == true || _azureOpts.IsConfigured;

    /// <summary>Human-readable label of the active provider for the UI.</summary>
    public string ActiveProviderLabel
    {
        get
        {
            var t = _settings.Current.AiProvider;
            if (t?.IsConfigured == true) return $"{t.Provider} / {t.ChatModel}";
            if (_azureOpts.IsConfigured)  return $"Azure AI / {_azureOpts.OpenAIModel}";
            return "Not configured";
        }
    }
}
