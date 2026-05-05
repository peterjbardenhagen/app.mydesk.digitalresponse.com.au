using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

public class CookieConsentService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieConsentService> _logger;

    public CookieConsentService(IHttpContextAccessor httpContextAccessor, ILogger<CookieConsentService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public bool IsConsentGiven()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Request.Cookies["cookie_consent"] != null)
                return true;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cookie consent");
            return false;
        }
    }

    public void GiveConsent()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Append("cookie_consent", "true", new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddYears(1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error giving cookie consent");
        }
    }

    public void RevokeConsent()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Delete("cookie_consent");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error revoking cookie consent");
        }
    }
}
