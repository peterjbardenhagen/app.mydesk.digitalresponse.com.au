using Techlight.MyDesk.MCP.Models;
using Techlight.MyDesk.MCP.Services;

namespace Techlight.MyDesk.MCP.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UserService userService)
    {
        // Skip authentication for health check
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Get API Key from header
        string? userCode = null;
        if (context.Request.Headers.TryGetValue("X-User-Code", out var codeValues))
        {
            userCode = codeValues.FirstOrDefault();
        }
        
        User? user = null;
        if (!string.IsNullOrEmpty(userCode))
        {
            user = await userService.GetUserByCodeAsync(userCode);
        }
        else if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues) && 
                 !string.IsNullOrEmpty(apiKeyValues.FirstOrDefault()))
        {
            user = await userService.GetUserFromApiKeyAsync(apiKeyValues.FirstOrDefault()!);
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                 authHeader.FirstOrDefault()?.StartsWith("Bearer ") == true)
        {
            var apiKey = authHeader.FirstOrDefault()![7..]; // Remove "Bearer "
            user = await userService.GetUserFromApiKeyAsync(apiKey);
        }

        if (user == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required (X-API-Key, Authorization, or X-User-Code header)" });
            return;
        }

        // Get user's accessible divisions
        var divisions = await userService.GetUserAccessibleDivisionsAsync(user.Code);

        // Create MCP Context
        var mcpContext = new McpContext
        {
            UserCode = user.Code,
            UserName = user.Name,
            IsAdmin = user.IsAdmin,
            AccessibleDivisions = divisions
        };

        // Store in HttpContext.Items for access in endpoints
        context.Items["McpContext"] = mcpContext;

        _logger.LogInformation("Authenticated request from user {UserCode}", user.Code);

        await _next(context);
    }
}
