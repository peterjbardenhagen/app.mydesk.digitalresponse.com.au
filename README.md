# DR MyDesk — Business Management Platform

**Version 3.0** · Powered by [Digital Response](https://www.digitalresponse.com.au/)

A comprehensive business operations platform for quotes, invoices, purchase orders, CRM, and business intelligence — with AI-powered insights and MYOB integration.

---

## Quick Start

### Easy Launch (Interactive Menu)

```batch
.\Run.bat
```

**Options:**
- **1** = Database setup (Access → SQL, requires Admin)
- **2** = Build & deploy to IIS (requires Admin, opens http://localhost)
- **3** = Run Playwright tests (requires Admin)
- **4** = **Run DR MyDesk** — Choose:
  - Standalone server (Kestrel) → http://localhost:5235
  - Local IIS → http://localhost (requires Admin + prior deploy)

### Alternative: PowerShell Setup

```powershell
# Run as Administrator for database, IIS, or tests
.\Setup.ps1
```

---

## What DR MyDesk Does

DR MyDesk manages your entire business operations:

- **Quotes** — Create, track, email, and convert to invoices
- **Invoices** — Generate, track payments, MYOB sync
- **Purchase Orders** — Procurement, approval workflows, supplier management
- **Contacts & Companies** — Full CRM with customer/supplier tracking
- **Despatch** — Delivery tracking and shipment management
- **Products** — Inventory and pricing management
- **Business Intelligence** — Performance targets, customer scoring, team leaderboards
- **Marketing** — Customer data platform, AI-driven campaigns, brand assets
- **Ask AI** — Natural language queries over your business data
- **Reports** — Revenue, profit, customer analysis, supplier spend

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8 Blazor Server |
| UI Library | MudBlazor 8.x |
| Database | SQL Server (LocalDB for dev) |
| Authentication | Cookie-based ASP.NET Core |
| AI | Azure OpenAI (GPT-4 Turbo) |
| Integration | MYOB AccountRight API |
| Deployment | IIS + ASP.NET Core Module |

---

## Project Structure

```
C:\Development\Techlight.digitalresponse.com.au\
├── src\                              # Main source code
│   ├── MyDesk.Web\                   # Blazor Server app
│   │   ├── Components\
│   │   │   ├── Layout\               # MainLayout, NavMenu
│   │   │   ├── Pages\                # All Razor pages
│   │   │   └── Shared\               # Reusable components
│   │   ├── Services\                 # UI services (AI, Email, PDF, etc.)
│   │   ├── Config\                   # JSON configs (navmenu, targets, etc.)
│   │   └── wwwroot\css\app.css       # Design system
│   │
│   ├── MyDesk.Shared\                # Shared library
│   │   ├── Models\                   # Domain models
│   │   └── Services\                 # Business logic
│   │
│   ├── Deployment\                   # Deployment scripts
│   │   ├── Deploy.ps1                # IIS deployment (run as Admin)
│   │   └── Migration\                # SQL migration scripts
│   │
│   ├── Documentation\                # Technical docs
│   ├── Run.bat                       # Interactive menu launcher
│   └── MyDesk.slnx                # Solution file
│
├── tests\                            # Playwright E2E tests
│   └── MyDesk.PlaywrightTests\
│
├── Run.bat                           # Root launcher (menu)
└── README.md                         # This file
```

---

## Running DR MyDesk

### Prerequisites
- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server LocalDB** — Included with Visual Studio or SQL Server Express
- **Visual Studio 2022** or **VS Code** (optional)

### Configuration

Update `src\MyDesk.Web\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TechlightDb": "Server=(localdb)\\MSSQLLocalDB;Database=Techlight_MyDesk;Trusted_Connection=True;"
  },
  "Azure": {
    "OpenAIApiKey": "your-azure-openai-key",
    "OpenAIEndpoint": "https://your-resource.openai.azure.com",
    "OpenAIModel": "gpt-4-turbo"
  }
}
```

### Run Locally

**Quick Launch (Recommended)**
```batch
.\Run.bat
# Choose option 4, then:
#   1 = Standalone server (http://localhost:5235) - Quick, no admin needed
#   2 = Local IIS (http://localhost) - Requires admin + prior deploy
```

**Manual (Power Users)**
```batch
cd src\MyDesk.Web
dotnet run
```
Access at: http://localhost:5235

---

## Database Setup

DR MyDesk uses SQL Server. On first run, the application will create tables automatically if they don't exist.

### Migration from Access (Legacy)

If migrating from the old Classic ASP + Access version:

1. Run the migration scripts in `src\Deployment\Migration\`
2. See `src\Deployment\Migration\README.md` for details

### Fresh Install

No migration needed — the app creates its schema on startup.

---

## Running Tests

DR MyDesk has **72+ Playwright end-to-end tests** covering all major workflows.

### Quick Test Run

```batch
.\Run.bat
# Choose option 3: Tests (requires Administrator)
```

### Manual Test Run

```batch
cd tests\MyDesk.PlaywrightTests
dotnet test
```

### Test Coverage
- Login/Authentication: 100%
- Dashboard: 95%
- Quotes, Invoices, POs: 90%+
- Navigation: 100%
- End-to-End Workflows: 70%

See **TESTING.md** for full documentation.

---

## Deployment to Production

### 1. Local IIS Deployment

```batch
.\Run.bat
# Choose option 2: IIS - Build and Deploy (requires Administrator)
```

Or use PowerShell directly:
```powershell
# Run as Administrator
cd src\Deployment
.\Deploy.ps1
```

This will:
- Build the Release version
- Create IIS App Pool and Site
- Deploy to `C:\inetpub\wwwroot\Techlight.MyDesk`
- Start the site on **http://localhost** (auto-opens in browser)

### 2. Production Server (VM)

1. Copy `src\Deployment\publish\` folder to server
2. Run `Deploy.ps1` as Administrator on the server
3. Update `appsettings.Production.json` with production connection string
4. Configure SSL certificate in IIS

---

## Key Features

### Dashboard
- Executive KPI cards (Revenue, Quotes, Profit, Health Score)
- 6-view business intelligence carousel (Directors only):
  - Performance Targets (Company/Team/Individual)
  - Customer Intelligence (Best/Growing/At-Risk)
  - Revenue Trends, Profit Analysis, Team Leaderboard, Comparative Analysis
- Personal KPI summary for all users
- Smart business warnings and recommendations

### Marketing Module (NEW)
- **Customer Data Platform** — RFM scoring, segmentation (Champions, At-Risk, Lost)
- **Supplier Data Platform** — Tier suppliers by spend, dependency, reliability
- **Marketing AI** — Generate ideal customer profiles, marketing plans, top-50 target lists
- **Email Campaigns** — Send to Champions, Top-50, or custom audiences
- **Marketing Strategy** — Document positioning, initiatives, KPIs
- **Brand Assets** — Logos, guidelines, company profile (secure download)

### Ask AI
- Natural language queries: _"What's our cash position?"_
- Integrated with MYOB data (customers, invoices, bank balance)
- Compliance audit log for all interactions

### MYOB Integration
- Sync customers, invoices, payments
- Real-time bank balance
- Profit & loss reports
- Automatic reconciliation

---

## Brand Colors

- **Primary Teal**: `#00c8c8`
- **Accent Gold**: `#cca05a`
- **Dark Navy**: `#08121a`

---

## Support & Documentation

- **Email**: info@digitalresponse.com.au
- **Hours**: Monday–Friday, 9am–5pm AEST
- **Documentation**: See `src\Documentation\` folder
- **Testing Guide**: See `TESTING.md`
- **Deployment Guide**: See `src\Deployment\README.md`

---

## Legal & Compliance

- **Privacy Policy**: `/privacy-policy` — Australian Privacy Act compliant
- **Terms & Conditions**: `/terms-and-conditions`
- **Copyright**: © 2026 Digital Response. All rights reserved.
- **Data Retention**: GDPR/Australian Privacy compliant

---

**Version**: 3.0.0  
**Release Date**: April 2026  
**Platform**: .NET 8 Blazor Server
