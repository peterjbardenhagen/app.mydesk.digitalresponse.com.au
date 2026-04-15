using System.Text.Json;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP;

public static class EndpointMappings
{
    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        // MCP Initialize Endpoint
        app.MapPost("/mcp/v1/initialize", async (McpRequest request, McpServer mcpServer) =>
        {
            var result = new McpInitializeResult
            {
                Capabilities = new McpCapabilities
                {
                    Tools = new McpToolsCapability { ListChanged = true }
                }
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = result
            };
        });

        // MCP Tools List Endpoint
        app.MapPost("/mcp/v1/tools/list", (McpRequest request, McpServer mcpServer) =>
        {
            var tools = mcpServer.GetAvailableTools();
            
            return new McpResponse
            {
                Id = request.Id,
                Result = new McpToolsListResult { Tools = tools }
            };
        });

        // MCP Tools Call Endpoint
        app.MapPost("/mcp/v1/tools/call", async (McpRequest request, McpServer mcpServer, HttpContext httpContext) =>
        {
            var context = httpContext.Items["McpContext"] as McpContext 
                ?? throw new InvalidOperationException("MCP Context not found");

            var toolCall = JsonSerializer.Deserialize<McpToolCallRequest>(
                JsonSerializer.Serialize(request.Params));

            var response = await mcpServer.HandleToolCallAsync(toolCall!, context);
            response.Id = request.Id;
            
            return response;
        });

        // Health check
        app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

        // Simple REST API endpoints for direct access (non-MCP)
        MapRestEndpoints(app);

        return app;
    }

    private static void MapRestEndpoints(IEndpointRouteBuilder app)
    {
        // REST API for Quotes
        app.MapGet("/api/quotes", async (QuoteService service, HttpContext httpContext,
            DateTime? from, DateTime? to, string? customer, string? status, int? limit) =>
        {
            var context = GetContext(httpContext);
            var quotes = await service.GetQuotesAsync(from, to, customer, status, null, limit, context);
            return Results.Ok(quotes);
        });

        app.MapGet("/api/quotes/{id:int}", async (int id, QuoteService service, HttpContext httpContext) =>
        {
            var context = GetContext(httpContext);
            var quote = await service.GetQuoteByIdAsync(id, context);
            return quote != null ? Results.Ok(quote) : Results.NotFound();
        });

        // REST API for Invoices
        app.MapGet("/api/invoices", async (InvoiceService service, HttpContext httpContext,
            DateTime? from, DateTime? to, string? customer, int? limit) =>
        {
            var context = GetContext(httpContext);
            var invoices = await service.GetInvoicesAsync(from, to, customer, limit: limit, context: context);
            return Results.Ok(invoices);
        });

        // REST API for Purchase Orders
        app.MapGet("/api/purchase-orders", async (PurchaseOrderService service, HttpContext httpContext,
            DateTime? from, DateTime? to, string? supplier, int? limit) =>
        {
            var context = GetContext(httpContext);
            var pos = await service.GetPurchaseOrdersAsync(from, to, supplier, limit: limit, context: context);
            return Results.Ok(pos);
        });

        // REST API for Contacts
        app.MapGet("/api/contacts/search", async (string name, ContactService service, HttpContext httpContext) =>
        {
            var context = GetContext(httpContext);
            var contacts = await service.SearchContactsByNameAsync(name, context);
            return Results.Ok(contacts);
        });

        // User info
        app.MapGet("/api/me", (HttpContext httpContext) =>
        {
            var context = GetContext(httpContext);
            return Results.Ok(new
            {
                code = context.UserCode,
                name = context.UserName,
                is_admin = context.IsAdmin,
                divisions = context.AccessibleDivisions
            });
        });
    }

    private static McpContext GetContext(HttpContext httpContext)
    {
        return httpContext.Items["McpContext"] as McpContext 
            ?? throw new InvalidOperationException("MCP Context not found - ensure authentication middleware is configured");
    }
}
