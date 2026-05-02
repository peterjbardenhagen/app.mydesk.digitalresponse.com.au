## System Overview
MyDesk is a monolithic Blazor Server application with a layered architecture:
- **UI Layer**: Razor components (.razor) using MudBlazor component library
- **Service Layer**: Business logic encapsulated in service classes (DatabaseService, TimesheetService, etc.)
- **Data Layer**: Direct SQL queries via DatabaseService (no EF Core, uses Dapper for some operations)

## Components
### API
No external API layer - Blazor Server uses SignalR under the hood for real-time communication.

### Workers
No background workers currently implemented.

### UI
- **MainLayout.razor**: Shell with navigation drawer and header
- **Pages/**: Feature-based Razor components (Timesheets, Contacts, Companies, Quotes, Invoices, etc.)
- **Shared Components**: Reusable UI pieces (FavouriteButton, etc.)

### Ingestion
Direct SQL Server data access via DatabaseService.

## Data Flow
```
User → Blazor UI (Razor) → Service Layer (Business Logic) → DatabaseService → SQL Server
                                    ↓
                              Domain Models (MyDesk.Shared.Models)
```

## Architectural Constraints
- No business logic in Razor components (keep in services)
- Domain layer has no framework dependencies (pure C# models)
- Services use DatabaseService for all data access (no direct DB calls from UI)
- Use Dictionary<string, object?> for parameterized SQL queries
