# Techlight MyDesk - Blazor Server Application

Enterprise business management system for Techlight Pty Ltd. Built with .NET 8 Blazor Server, MudBlazor UI components, and SQL Server database.

## What This Application Is

MyDesk is Techlight's internal business operations platform that manages:

- **Quotes** - Create, track, and manage customer quotations
- **Invoices** - Generate invoices and track payments
- **Purchase Orders** - Manage procurement and supplier orders
- **Contacts & Companies** - Customer and supplier relationship management
- **Despatch** - Track deliveries and shipments
- **Reporting** - Business intelligence and analytics

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8 Blazor Server |
| UI Library | MudBlazor 8.x |
| Database | SQL Server (legacy schema) |
| Authentication | ASP.NET Core Identity |
| Styling | Custom CSS with Techlight brand colors |

## Brand Colors

- **Primary Teal**: `#00c8c8`
- **Accent Gold**: `#cca05a`
- **Dark Navy**: `#08121a`

## Project Structure

```
/src/Techlight.MyDesk.Web/
  /Components/
    /Layout/           - MainLayout, navigation
    /Pages/            - Razor pages (Quotes, Invoices, POs, etc.)
    /Shared/           - Reusable components
  /wwwroot/css/        - app.css with design system
  /Services/           - UI-related services

/src/Techlight.MyDesk.Shared/
  /Models/             - Domain models (Quote, Invoice, etc.)
  /Services/           - Business logic services

/Database/             - SQL scripts and schema
/Documentation/        - Technical documentation
```

## Running the Application

### Prerequisites
- .NET 8 SDK
- SQL Server (or LocalDB)
- Visual Studio 2022 or VS Code

### Configuration
Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=Techlight;Trusted_Connection=True;"
  }
}
```

### Run Locally
```bash
cd src/Techlight.MyDesk.Web
dotnet run
```

Access at: `http://localhost:5235`

## Database

The application uses the existing Techlight SQL Server database. Connection is configured via `appsettings.json`. No migrations required - works with legacy schema.

## Key Features

### Dashboard
- Executive KPI cards (Revenue, Quotes, Profit Margin, Health Score)
- Interactive carousel with 6 business intelligence views
- Team performance leaderboard (Directors/Admins)
- Personal KPI cards for all users
- Business warnings and smart recommendations

### Quotes Management
- Create quotes with line items
- Track quote status (Open, Won, Lost)
- Email quotes to customers
- Convert quotes to invoices

### Invoice Management
- Generate invoices from quotes
- Track payment status
- Overdue invoice alerts
- MYOB export capability

### Purchase Orders
- Create purchase orders
- Approval workflow
- Track PO status

### User Experience
- Dark sidebar navigation
- Responsive design
- Quick action buttons
- Real-time business metrics

## Running Tests

### Playwright End-to-End Tests

We have comprehensive Playwright test coverage (72+ tests):

```bash
# Run all tests interactively
.\Run-Tests.bat

# Or run specific test categories
dotnet test tests/MyDesk.PlaywrightTests --filter "FullyQualifiedName~LoginTests"
dotnet test tests/MyDesk.PlaywrightTests --filter "FullyQualifiedName~DashboardTests"
dotnet test tests/MyDesk.PlaywrightTests --filter "FullyQualifiedName~EndToEndWorkflowTests"
```

**Test Coverage:**
- Login/Authentication (100%)
- Dashboard (95%)
- Quotes, Invoices, Purchase Orders (90%+)
- Contacts, Companies, Products (80%+)
- Navigation (100%)
- Accessibility (75%)
- End-to-End Workflows (70%)

See `tests/MyDesk.PlaywrightTests/USAGE.md` for detailed documentation.

## Deployment

### Production Build
```bash
dotnet publish -c Release
```

### IIS Deployment
1. Publish to folder
2. Create IIS Application
3. Configure Application Pool (.NET CLR Version: No Managed Code)
4. Set connection string in `appsettings.Production.json`

## Support

- **Email**: info@digitalresponse.com.au
- **Hours**: Monday-Friday, 9am-5pm AEST

---

**Version**: 2026.04 - Blazor Server Release  
**Last Updated**: April 2026
