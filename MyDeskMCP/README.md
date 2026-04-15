# Techlight MyDesk MCP Server

This is a Model Context Protocol (MCP) server that enables Claude Desktop (and other MCP clients) to interact with your Techlight MyDesk application. You can query information about Quotes, Invoices, Purchase Orders, Contacts, and Users, as well as perform actions like creating quotes, updating statuses, and sending emails.

## Features

### Available Tools

#### Quotes
- `get_quote` - Get detailed information about a specific quote
- `list_quotes` - List quotes with filters (date range, customer, status)
- `create_quote` - Create a new quote with line items
- `update_quote_status` - Update the status of a quote (Draft, Pending, Won, Lost, etc.)
- `email_quote` - Email a quote to a recipient with optional PDF attachment
- `generate_quote_report` - Generate a report of quotes for a date range

#### Invoices
- `get_invoice` - Get detailed information about an invoice
- `list_invoices` - List invoices with filters
- `get_latest_invoices` - Get invoices from the last N days (default 30)
- `generate_invoice_report` - Generate invoice reports by customer, date range, or originator

#### Purchase Orders
- `get_purchase_order` - Get details about a purchase order
- `list_purchase_orders` - List POs with filters
- `update_po_status` - Update PO status (Draft, Pending, Ordered, Partially Received, Received, Cancelled, Completed)

#### Contacts
- `get_contact` - Get contact details by ID
- `search_contacts` - Search for contacts by name

#### Users
- `get_user_info` - Get current authenticated user information

## Installation

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (or existing MyDesk database)
- Claude Desktop application

### Step 1: Build the MCP Server

```powershell
# Navigate to the MCP directory
cd "C:\Development\Techlight.digitalresponse.com.au\MyDeskMCP"

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Publish for production
dotnet publish -c Release -o ./publish
```

### Step 2: Configure the Database Connection

Edit `appsettings.json` to set your database connection string:

```json
{
  "ConnectionStrings": {
    "MyDeskDatabase": "Server=localhost;Database=MyDesk;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Step 3: Set Up API Keys

You need to create an API Keys table in your database and add your first API key:

```sql
-- Create API Keys table if not exists
CREATE TABLE ApiKeys (
    ApiKeyId INT IDENTITY(1,1) PRIMARY KEY,
    ApiKey NVARCHAR(255) NOT NULL UNIQUE,
    UserId INT NOT NULL,
    Name NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    ExpiresAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Create Email Log table for tracking
CREATE TABLE EmailLog (
    EmailLogId INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT,
    ToEmail NVARCHAR(255),
    Subject NVARCHAR(500),
    SentBy NVARCHAR(50),
    SentDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50)
);

-- Insert your first API key
INSERT INTO ApiKeys (ApiKey, UserId, Name, IsActive)
VALUES ('your-secure-api-key-here', 1, 'Claude Desktop', 1);
```

Generate a secure API key:
```powershell
# Generate a secure random API key
$apiKey = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object { [char]$_ })
Write-Host "Your API Key: $apiKey"
```

### Step 4: Configure Email (Optional)

Edit `appsettings.json` to configure SMTP settings for sending quote emails:

```json
{
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": "587",
    "SmtpUser": "your-email@techlight.com.au",
    "SmtpPass": "your-password-or-app-specific-password",
    "FromAddress": "your-email@techlight.com.au"
  }
}
```

### Step 5: Run the Server

```powershell
# Run locally for development
dotnet run

# Or run the published version
cd ./publish
./MyDeskMCP.exe
```

The server will start on `http://localhost:5000` and `https://localhost:5001`.

### Step 6: Configure Claude Desktop

1. Open Claude Desktop
2. Go to Settings (gear icon) → Developer → Edit Configuration
3. Open the `claude_desktop_config.json` file
4. Add the MCP server configuration:

```json
{
  "mcpServers": {
    "techlight-mydesk": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Development\\Techlight.digitalresponse.com.au\\MyDeskMCP\\MyDeskMCP.csproj"
      ],
      "env": {
        "MCP_API_KEY": "your-secure-api-key-here"
      }
    }
  }
}
```

For the published version:

```json
{
  "mcpServers": {
    "techlight-mydesk": {
      "command": "C:\\Development\\Techlight.digitalresponse.com.au\\MyDeskMCP\\publish\\MyDeskMCP.exe",
      "env": {
        "MCP_API_KEY": "your-secure-api-key-here"
      }
    }
  }
}
```

5. Save the file and restart Claude Desktop

### Step 7: Verify the Connection

After restarting Claude Desktop, you should see a tool icon (🔧) in the chat input area. Click it to see the available MyDesk tools.

## Usage Examples

### Get Quote Information

"Can you get me details on Quote 12345?"

### Create a New Quote

"Create a new quote for Contact ID 5678 with these line items:
- 10 x LED Panel Light - $150 each cost, $250 each sell
- 5 x Power Supply Unit - $80 each cost, $120 each sell
- Reference: Smith Office Lighting Project"

### Generate Reports

"Can you give me a report of all quotes from the past 30 days?"

"Show me all invoices from Peter John Bardenhagen from January to March 2024"

"Generate a quote report for customer 'ABC Company' from 2024-01-01 to 2024-03-31"

### Email Quotes

"Can you email Quote 123 to admin@techlight.com.au?"

"Send Quote 456 to the customer contact with a note saying 'Please find attached your revised quote'"

### Update Statuses

"Update Purchase Order 789 status to 'Ordered' with a note 'Ordered from supplier today'"

"Change Quote 555 status to 'Won'"

### Search Contacts

"Search for contacts with the name 'Smith'"

"Find me the contact details for John at ABC Lighting"

## API Endpoints

The server also exposes a REST API for direct integration:

### Authentication
All requests must include the API key header:
```
X-API-Key: your-secure-api-key-here
```

Or in the Authorization header:
```
Authorization: Bearer your-secure-api-key-here
```

### Available Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check |
| GET | `/api/me` | Get current user info |
| GET | `/api/quotes` | List quotes (query params: from, to, customer, status, limit) |
| GET | `/api/quotes/{id}` | Get specific quote |
| GET | `/api/invoices` | List invoices |
| GET | `/api/purchase-orders` | List purchase orders |
| GET | `/api/contacts/search?name={name}` | Search contacts |

### MCP Protocol Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/mcp/v1/initialize` | Initialize MCP connection |
| POST | `/mcp/v1/tools/list` | List available tools |
| POST | `/mcp/v1/tools/call` | Call a tool |

## Security Considerations

1. **API Keys**: Store API keys securely. Never commit them to version control.

2. **Database Connection**: Use Windows Authentication (Trusted_Connection) where possible. If using SQL authentication, store credentials in environment variables or Azure Key Vault.

3. **Rate Limiting**: Consider adding rate limiting for production use.

4. **HTTPS**: Always use HTTPS in production. Configure a valid SSL certificate.

5. **CORS**: Configure CORS appropriately if accessing from web applications.

## Troubleshooting

### Claude Desktop doesn't see the tools

1. Check the MCP server is running: `http://localhost:5000/health`
2. Verify the API key is correct in the configuration
3. Check Claude Desktop logs: `%APPDATA%\Claude\logs\mcp.log`
4. Restart Claude Desktop after configuration changes

### Database connection errors

1. Verify the connection string in `appsettings.json`
2. Ensure SQL Server is running and accessible
3. Check firewall settings for port 1433
4. Verify the user has appropriate permissions

### Email sending fails

1. Check SMTP settings in `appsettings.json`
2. Verify credentials are correct
3. Check if SMTP port is blocked by firewall
4. Review application logs for detailed error messages

## Development

### Project Structure

```
MyDeskMCP/
├── MyDeskMCP.csproj          # Project file
├── Program.cs                # Entry point
├── appsettings.json          # Configuration
├── McpServer.cs              # MCP protocol implementation
├── EndpointMappings.cs       # HTTP endpoint definitions
├── Models/
│   ├── McpModels.cs         # MCP protocol models
│   └── DomainModels.cs       # Quote, Invoice, PO models
├── Services/
│   ├── DatabaseService.cs     # Database access
│   ├── QuoteService.cs        # Quote operations
│   ├── InvoiceService.cs      # Invoice operations
│   ├── PurchaseOrderService.cs # PO operations
│   ├── ContactService.cs      # Contact operations
│   ├── UserService.cs         # User & auth operations
│   └── EmailService.cs        # Email operations
└── Middleware/
    └── AuthenticationMiddleware.cs # API key validation
```

### Adding New Tools

1. Define the tool schema in `McpServer.cs` → `GetAvailableTools()`
2. Implement the handler in `McpServer.cs`
3. Add corresponding service method if needed
4. Test with Claude Desktop

### Testing

```powershell
# Test the health endpoint
Invoke-RestMethod -Uri "http://localhost:5000/health"

# Test authentication
Invoke-RestMethod -Uri "http://localhost:5000/api/me" -Headers @{"X-API-Key"="your-api-key"}
```

## Support

For issues or questions:
1. Check the logs in the `Logs` folder
2. Review the troubleshooting section above
3. Contact your system administrator

## License

This MCP server is proprietary software for Techlight MyDesk integration.
