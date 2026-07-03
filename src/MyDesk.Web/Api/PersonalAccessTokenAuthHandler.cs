using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;
using MyDesk.Web.Services;

namespace MyDesk.Web.Api;

/// <summary>
/// Authentication scheme that validates <c>Authorization: Bearer mdk_xxx</c> tokens
/// issued by <see cref="PersonalAccessTokenService"/>.
///
/// On success, sets claims that match what <see cref="CurrentTenantAccessor"/> expects:
///   tenant_id    → ICurrentTenantAccessor.TenantId
///   tenant_name  → ICurrentTenantAccessor.TenantName
///   UserId       → ICurrentTenantAccessor.UserId
///   NameIdentifier (ClaimTypes) → ICurrentTenantAccessor.UserCode
///   Name (ClaimTypes)           → display name
/// </summary>
public sealed class PatAuthOptions : AuthenticationSchemeOptions { }

public sealed class PersonalAccessTokenAuthHandler : AuthenticationHandler<PatAuthOptions>
{
    public const string SchemeName = "PersonalAccessToken";

    private readonly PersonalAccessTokenService _tokenSvc;

    public PersonalAccessTokenAuthHandler(
        IOptionsMonitor<PatAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PersonalAccessTokenService tokenSvc)
        : base(options, logger, encoder)
    {
        _tokenSvc = tokenSvc;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer mdk_", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawToken = authHeader["Bearer ".Length..].Trim();
        var token = await _tokenSvc.ValidateAsync(rawToken);
        if (token is null)
            return AuthenticateResult.Fail("Invalid, revoked, or expired personal access token.");

        var claims = new List<Claim>
        {
            // Tenant isolation claims — read by CurrentTenantAccessor
            new(TenantConstants.TenantIdClaim,   token.TenantId.ToString()),
            new(TenantConstants.TenantNameClaim,  token.TenantName ?? ""),

            // User identity claims
            new("UserId",                          token.UserId.ToString()),
            new(ClaimTypes.NameIdentifier,         token.UserCode ?? ""),
            new(ClaimTypes.Name,                   token.UserName ?? ""),

            // Scopes stored as a single claim (space-separated, same as OAuth convention)
            new("token_scopes", token.Scopes),
        };

        // Map the user's platform role to an ASP.NET role claim
        if (!string.IsNullOrWhiteSpace(token.UserRole))
            claims.Add(new Claim(ClaimTypes.Role, token.UserRole));

        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
