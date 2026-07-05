using MyDesk.Web.Services;

namespace MyDesk.Web.Middleware;

/// <summary>
/// Middleware for database-backed rate limiting with security enforcement
/// Works in conjunction with ASP.NET Core's built-in rate limiting
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Endpoints that require rate limiting bypass authentication
    private static readonly string[] PublicEndpoints = [
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/tenant/resolve-domain",
        "/api/tenant/verify-domain-status",
        "/api/tenant/verify-domain"
    ];

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RateLimitingService rateLimitingService)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Skip rate limiting for GET requests and non-API endpoints
        if (method == "GET" || !path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        // Get identifier (IP for public endpoints, UserId for authenticated)
        string identifier = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string identifierType = "IP";

        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var userId = context.User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                identifier = userId;
                identifierType = "USER";
            }
        }

        // Check if IP is auto-blocked
        if (identifierType == "IP")
        {
            bool isBlocked = await rateLimitingService.IsIpBlockedAsync(identifier);
            if (isBlocked)
            {
                _logger.LogWarning("Request blocked: IP {Ip} is auto-blocked", identifier);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Add("Retry-After", "3600");
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
                return;
            }
        }

        // Check rate limit for this endpoint
        var (allowed, remaining, retryAfter) = await rateLimitingService.CheckRateLimitAsync(
            identifier,
            path,
            identifierType);

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded: {IdentifierType}={Identifier} on {Path}. Retry after {RetryAfter}s",
                identifierType, identifier, path, retryAfter);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", retryAfter.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                retryAfter,
                message = $"Please wait {retryAfter} seconds before trying again."
            });
            return;
        }

        // Add rate limit info to response headers
        if (remaining >= 0)
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
                return Task.CompletedTask;
            });

        await _next(context);
    }
}
