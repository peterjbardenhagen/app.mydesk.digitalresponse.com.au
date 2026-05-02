# MyDesk

## Purpose
MyDesk is a comprehensive business management platform for consulting companies. It provides CRM, sales, purchasing, operations, and financial management capabilities in a single integrated solution. Success looks like: consultants efficiently tracking billable/non-billable time, managing customer/supplier relationships, processing quotes and invoices, and accessing real-time business insights—all from a unified, modern web interface.

## High-Level Architecture
- **MyDesk.Web**: Blazor Web App (server-side) - main UI layer with Razor components
- **MyDesk.Shared**: Core business logic, services, and models shared across the platform
- **Database**: SQL Server backend with direct SQL queries (no ORM - uses Dapper/DatabaseService)

## Tech Stack
- **Backend**: .NET 10 (preview), C# 12, Blazor Server
- **Frontend**: MudBlazor v7.15.0 (material design components), Razor components
- **Infra**: IIS/Kestrel hosting, Windows environment
- **Data**: SQL Server, Microsoft.Data.SqlClient, Dapper (lightweight ORM)

## Running the Project
```bash
# Restore dependencies
dotnet restore

# Run with hot-reload (development)
dotnet watch --project src/MyDesk.Web/MyDesk.Web.csproj

# Run via Kestrel (production-like)
Run.bat (option 4)

# Build
dotnet build src/MyDesk.Web/MyDesk.Web.csproj
```

## Non-Goals
- Not a SaaS platform (on-premise/deployable on air-gapped networks)
- Not a replacement for full ERP (focused on consulting company needs)
- No multi-tenant architecture (single organisation per deployment)
- No mobile-native apps (responsive web only)
