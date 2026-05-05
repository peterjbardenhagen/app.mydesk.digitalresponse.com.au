using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Api;

/// <summary>
/// Header-based API key authentication for external products integrating with MyDesk.
///
/// Header: <c>X-Api-Key: &lt;configured-key&gt;</c> (configured under <c>Api:Key</c>
/// in appsettings — production should issue per-tenant keys via a future ApiKeys table).
///
/// On success, the principal is given the <c>ApiClient</c> role plus the Techlight tenant
/// claim by default. Endpoints can additionally specify <c>?tenantId=...</c> to scope queries.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    private readonly IConfiguration _config;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
            string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredKey = _config["Api:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key auth is not configured (Api:Key empty)."));
        }
        if (!string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var tenantIdRaw = Request.Query["tenantId"].ToString();
        var tenantId = Guid.TryParse(tenantIdRaw, out var t) ? t : TenantConstants.TechlightTenantId;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "api-client"),
            new Claim(ClaimTypes.NameIdentifier, "api-client"),
            new Claim(ClaimTypes.Role, "ApiClient"),
            new Claim(TenantConstants.TenantIdClaim, tenantId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
