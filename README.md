# MyDesk

## Purpose
MyDesk is a comprehensive business management platform for consulting companies. It provides CRM, sales, purchasing, operations, and financial management capabilities in a single integrated solution with both web and mobile interfaces. Success looks like: consultants efficiently tracking billable/non-billable time, managing customer/supplier relationships, processing quotes and invoices, accessing real-time business insights—all from a unified, modern web app or native mobile app.

## High-Level Architecture
- **MyDesk.Web**: Blazor Web App (server-side) - main UI layer with Razor components
- **MyDesk.Mobile**: Native Android app with PAT authentication and offline support
- **MyDesk.Shared**: Core business logic, services, and models shared across platforms
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

## Available Modules

### Core (Web & Mobile)
- **Invoices** — customer billing, payment tracking, GST reporting
- **Quotes** — quote creation, expiry tracking, margin analysis
- **Purchase Orders** — supplier orders, delivery tracking
- **File Library** — document storage with metadata, folder structure

### Phase 1 (Web & Mobile)
- **Expenses** — employee expense tracking with receipts and categories
- **Timesheets** — weekly time entry, billable hours tracking
- **Tasks** — task management with priorities, assignments, due dates
- **Despatch** — delivery tracking with proof of delivery

### Phase 2 (Web & Mobile)
- **Contacts** — customer/supplier contact management
- **Cash Flow** — 12-week cash position forecasting
- **Business Goals** — KPI tracking with progress indicators
- **Projects** — project tracking with health status and milestones

## Mobile App
See [MOBILE-APP.md](MOBILE-APP.md) for:
- Architecture and authentication (PAT tokens)
- Module gating based on tenant
- API endpoints and data sync
- Building and deploying APK
- Development guide for adding new modules

## Non-Goals
- Not a SaaS platform (on-premise/deployable on air-gapped networks)
- Not a replacement for full ERP (focused on consulting company needs)
- No multi-tenant per user in single deployment (single org per instance)
