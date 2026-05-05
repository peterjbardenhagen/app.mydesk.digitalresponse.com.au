# MyDesk Platform Architecture

## Overview
MyDesk is a comprehensive Business Management Platform built with .NET 10 Blazor Server, delivering a unified interface for CRM, quoting, invoicing, purchase orders, job tracking, and more.

## Key Technologies
- **Framework**: .NET 10 Blazor Server
- **UI Component Library**: MudBlazor 7.x
- **Database**: SQL Server (Entity Framework / ADO.NET)
- **Logging**: Serilog
- **Authentication**: Cookie-based authentication with one-time-token handoff
- **Testing**: Playwright for End-to-End browser testing

## System Architecture

### 1. Presentation Layer (MyDesk.Web)
Built with Blazor Server, the UI is fully interactive with components streaming state over SignalR. The UI relies heavily on MudBlazor components for a consistent Material Design aesthetic. 
Key concepts:
- **InteractiveServerRenderMode**: For rich user interaction without writing JavaScript.
- **CascadingAuthenticationState**: Propagates user identity.
- **MudProviders**: Setup for MudBlazor theme, dialogs, and snackbars.

### 2. Business Logic Layer (MyDesk.Shared)
A shared class library containing all domain models, database abstractions, and core service definitions.
Key services:
- **PermissionService**: Granular access control, resolving user access based on role definitions.
- **TenantService**: Handles multi-tenancy requirements and isolates data where necessary.
- **DatabaseService**: Manages scoped database interactions.
- **PdfService**: Uses QuestPDF to generate beautiful PDF outputs for quotes/invoices.
- **EmailService**: Integrates with SendGrid for transactional emails.

### 3. Data Layer
SQL Server using ADO.NET and custom SQL statements for highly optimized queries. Tables are created automatically on startup by various domain services via idempotent `IF NOT EXISTS` scripts.

## Multi-Tenancy Architecture
MyDesk supports multi-tenancy. When a user logs in, they select their tenant if they belong to multiple. The `TenantService` and `PlatformSettingsService` configure the environment dynamically (e.g. colors, logos) for the active tenant.

## Security Model
1. **Authentication**: Cookie auth with sliding expiration.
2. **Authorization**: Handled via `[Authorize]` attributes and the robust `PermissionService`.
3. **Data Protection**: Anti-forgery tokens used for Blazor endpoints. All crawlers blocked via X-Robots-Tag and robots.txt.
