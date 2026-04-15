using Techlight.MyDesk.MCP;
using Techlight.MyDesk.MCP.Services;
using Techlight.MyDesk.MCP.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<QuoteService>();
builder.Services.AddSingleton<InvoiceService>();
builder.Services.AddSingleton<PurchaseOrderService>();
builder.Services.AddSingleton<ContactService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<EmailService>();

// Add MCP Server
builder.Services.AddSingleton<McpServer>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Add authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

// Map MCP endpoints
app.MapMcpEndpoints();

app.Run();
