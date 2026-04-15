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
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues) || 
            string.IsNullOrEmpty(apiKeyValues.FirstOrDefault()))
        {
            // Also try Authorization header
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
                !authHeader.FirstOrDefault()?.StartsWith("Bearer ") == true)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "API Key or Authorization header required" });
                return;
            }
            
            apiKeyValues = authHeader.FirstOrDefault()![7..]; // Remove "Bearer "
        }

        var apiKey = apiKeyValues.FirstOrDefault();

        // Validate API Key
        var user = await userService.GetUserFromApiKeyAsync(apiKey!);
        
        if (user == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
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
