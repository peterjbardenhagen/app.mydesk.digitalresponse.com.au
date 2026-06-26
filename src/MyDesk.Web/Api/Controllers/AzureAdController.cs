using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Azure AD / Microsoft Entra ID authentication endpoints.
/// Handles the OpenID Connect challenge flow for browser-based sign-in.
/// </summary>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AzureAdController : ControllerBase
{
    /// <summary>
    /// Initiates a Microsoft Entra ID (Azure AD) sign-in challenge.
    /// This endpoint is called by the login page "Sign in with Microsoft" button.
    /// </summary>
    /// <param name="redirectUri">Optional redirect URL after authentication succeeds (defaults to /)</param>
    [HttpGet("Challenge")]
    public IActionResult Challenge(string? redirectUri = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            redirectUri = "/";

        // Validate redirect URI to prevent open-redirect attacks
        if (!redirectUri.StartsWith("/") && !Uri.IsWellFormedUriString(redirectUri, UriKind.Relative))
            redirectUri = "/";

        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return new ChallengeResult("AzureAd", properties);
    }
}
