# Techlight MyDesk - System Documentation

## Overview

**Techlight MyDesk** is a legacy ASP Classic Customer Relationship Management (CRM) system that has been in continuous operation for over 20 years. It manages quotes, invoices, purchase orders, contacts, and sales operations for Techlight (a project lighting specialist company).

- **Platform**: ASP Classic (VBScript) with some ASP.NET interop components
- **Database**: Microsoft Access (Techlight2.mdb)
- **Web Server**: IIS on Windows virtual machine
- **Production URL**: https://techlight.digitalresponse.com.au
- **Legacy History**: Originally designed for multiple clients (SalesEngine) but now single-tenant for Techlight only

---

## Architecture

### High-Level Structure

```
┌─────────────────────────────────────────────────────────────┐
│                        IIS Server                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────┐  │
│  │   ASP Classic App │    │   ASP.NET App (Separate    │  │
│  │   (Main MyDesk)    │◄───│    Worker Process)         │  │
│  │                    │    │   - PDF Generation         │  │
│  │  /Clients/         │    │   - Document Rendering     │  │
│  │  SalesEngineTL/    │    │   - Email/Fax Processing   │  │
│  │  System/             │    │                            │  │
│  └─────────────────────┘    └─────────────────────────────┘  │
│           │                                                 │
│           ▼                                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Microsoft Access Database                    │  │
│  │              /Database/Techlight2.mdb                  │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Key Architectural Patterns

1. **Dual Application Architecture**: ASP Classic handles main UI/business logic; ASP.NET runs in separate app pool for PDF generation and modern processing
2. **Frames-Based UI**: Heavy use of HTML framesets and iframes for layout
3. **Cookie-Based Session Management**: Relies on cookies for user state (not server-side sessions)
4. **Include-Driven Modularization**: Common functionality via `<!--#include-->` directives

---

## Directory Structure

### Root Level

```
c:\Development\Techlight.digitalresponse.com.au\
├── App_Code/                 # .NET code-behind files (sparse)
├── App_Data/                 # ASP.NET data directory
├── bin/                      # Compiled .NET assemblies
├── Clients/                  # CLIENT-SPECIFIC CODE (main application)
│   ├── SalesEngine/          # Legacy multi-client shared code
│   └── SalesEngineTL/        # ★ Techlight-specific code (primary)
├── Database/                 # MS Access DB location (C:\Database in prod)
├── Errors/                   # Error handling pages
├── FusionChart/              # Charting components
├── Guest/                    # Public/unauthenticated pages
├── images/                   # Shared images
├── MyDeskASPNet/             # ★ ASP.NET interop application
├── obj/                      # Build artifacts
├── Properties/               # .NET project properties
├── Quotes/                   # (Empty - legacy)
├── SalesEngine/              # Legacy shared modules
├── System/                   # ★ Shared system files (core includes)
└── web.config               # IIS configuration
```

### Client-Specific Structure (`/Clients/SalesEngineTL/`)

```
SalesEngineTL/
├── Portal/                   # Authentication & entry point
│   ├── Validate.asp         # Login validation
│   ├── LogOff.asp           # Logout handler
│   ├── ChangePassword.asp   # Password management
│   └── AccessDenied.asp     # Permission error page
├── Header.asp               # Frame header (navigation, user info)
├── Default.asp              # Main frameset definition
├── Portal.asp               # Home dashboard
├── PortalFrame.asp          # Content frame wrapper
│
├── Quotes/                  # ★ QUOTE MANAGEMENT (core module)
│   ├── Default.asp          # Quote list/filter page
│   ├── IFrame.asp           # Quote list iframe content
│   ├── View.asp             # Quote display/print view
│   ├── Edit.asp             # Quote editing form
│   ├── Add.asp / Add2.asp   # Create new quote (2-step)
│   ├── Add_Proc.asp         # Quote creation processor
│   ├── Edit_Proc.asp        # Quote update processor
│   ├── Email.asp            # Email composition form
│   ├── Email_Proc.asp       # Email sending processor
│   ├── Fax.asp              # Fax composition form
│   ├── Fax_Proc.asp         # Fax sending processor
│   ├── Copy_Proc.asp        # Duplicate quote
│   ├── UpdateStatus.asp     # Status change UI
│   ├── GenerateQuote.asp    # PDF generation redirector
│   ├── NavBar.asp           # Action button bar
│   ├── Report.asp           # Reporting interface
│   └── Files/               # Generated PDF storage
│
├── Invoices/                # INVOICE MANAGEMENT
│   ├── Default.asp, View.asp, Edit.asp, Add.asp
│   ├── GenerateInvoice.asp/.aspx  # Invoice PDF generation
│   ├── GenerateDeliveryNote.asp   # Delivery note PDF
│   ├── EmailDeliveryNote.asp      # Delivery note email
│   └── (similar structure to Quotes)
│
├── PurchaseOrders/          # PURCHASE ORDER MANAGEMENT
│   ├── GeneratePO.aspx      # PO PDF generation
│   ├── GenerateFromRFQ.asp  # Create PO from RFQ
│   └── (similar structure)
│
├── RFQ/                     # REQUEST FOR QUOTATION
│   ├── GenerateRFQ.aspx/.vb # RFQ PDF generation (.NET)
│   ├── Compare.asp          # Supplier quote comparison
│   └── (similar structure)
│
├── Contacts/                # CONTACT/CRM MANAGEMENT
├── Companies/               # COMPANY/CLIENT MANAGEMENT
├── Products/                # PRODUCT CATALOG
├── Users/                   # USER MANAGEMENT
├── Divisions/               # DIVISION/BRANCH MANAGEMENT
├── Locations/               # LOCATION/WAREHOUSE MANAGEMENT
├── Timesheets/              # EMPLOYEE TIMESHEETS
├── Expenses/                # EXPENSE CLAIMS
├── CallReports/             # SALES ACTIVITY REPORTING
├── JobOrders/               # JOB/PROJECT MANAGEMENT
├── FilesLibrary/            # DOCUMENT MANAGEMENT
├── TMail/                   # INTERNAL MESSAGING
├── Reports/                 # BUSINESS REPORTS
├── Setup/                   # SYSTEM CONFIGURATION
└── ssi_Security.inc         # Client security include
```

### System Shared Files (`/System/`)

```
System/
├── ssi_Functions.asp        # ★ CORE FUNCTION LIBRARY (58KB)
├── ssi_dbConn_open.inc      # ★ Database connection opener
├── ssi_dbConn_open_TL.inc   # Techlight-specific DB connection
├── ssi_dbConn_close.inc     # Database connection closer
├── ssi_Security.inc         # Login check (redirects if not logged in)
├── ssi_Errors.asp           # Error handling
├── ssi_Dates.inc            # Date formatting/processing (11KB)
├── ssi_Header.inc           # Common header include
├── ADOVBS.inc               # ADO constants (7KB)
├── Var.asp                  # Client detection/set variables
├── IFrame.asp               # Generic iframe content handler
│
├── Global.js                # ★ CORE JAVASCRIPT LIBRARY (11KB)
├── grid.js / Grid2.js       # Data grid components (155KB)
├── cal2.js / cal_conf2.js   # Calendar/date picker
├── aw.js                    # ActiveWidgets grid library (155KB)
│
├── Style.css / Style2.css   # Stylesheets
├── grid.css                 # Grid-specific styles
├── PL_Style.css             # Pierlite legacy styles
└── TTL2.new.mdb             # (Empty placeholder)
```

### ASP.NET Interop Application (`/MyDeskASPNet/`)

```
MyDeskASPNet/
├── Web.config               # .NET configuration
├── packages.config          # NuGet packages
├── MyDeskASPNet.csproj      # Project file
│
├── GenerateQuote.aspx/.cs   # Quote PDF generation
├── GenerateInvoice.aspx/.cs   # Invoice PDF generation
├── GeneratePurchaseOrder.aspx/.cs  # PO PDF generation
├── GenerateDeliveryNote.aspx/.cs   # Delivery note PDF
├── ScrapeToPDF.aspx/.cs     # ★ HTML-to-PDF conversion engine
├── MakeThumbnails.aspx/.cs   # Image processing
└── UnitTests.aspx/.cs        # Unit testing page
```

---

## ASP Classic → ASP.NET Interoperability

### Pattern Overview

The system uses a **redirect-based interop pattern** where ASP Classic pages hand off processing to ASP.NET for PDF generation and other modern operations:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  ASP Classic    │     │   ASP.NET       │     │  ASP Classic    │
│  Page (Form)    │────►│   Handler       │────►│  Completion     │
│                 │     │                 │     │  Page           │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
   1. User submits       2. .NET generates      3. Redirect to
      form data             PDF via ABCpdf         confirmation
   4. Redirect to            saves to Files/        or email sent
      /MyDeskASPNet/
```

### Example: Quote Email/Fax Flow

**Step 1: User clicks Email on Quote**
- URL: `/Clients/SalesEngineTL/Quotes/Email.asp?Qid=5806`
- Shows email composition form

**Step 2: User submits form**
- Posts to: `GenerateQuote.asp`
- Validates data, updates quote status (Draft → Issued)

**Step 3: Redirect to .NET**
```vbscript
Response.Redirect("/MyDeskASPNet/GenerateQuote.aspx?Mode=1&Qid=5806&...")
```

**Step 4: .NET Processing (GenerateQuote.aspx.cs)**
```csharp
// Uses WebSupergoo.ABCpdf11 for PDF generation
Doc theDoc = new Doc();
theDoc.AddImageUrl(url);  // Scrapes View.asp page
File.WriteAllBytes(filePath, theDoc.GetData());
```

**Step 5: Return to ASP Classic for Email**
- Redirects to: `Email_Proc.asp`
- Sends email with PDF attachment

### Key Integration Points

| ASP Classic Page | ASP.NET Handler | Purpose |
|------------------|-----------------|---------|
| `Quotes/GenerateQuote.asp` | `MyDeskASPNet/GenerateQuote.aspx` | Quote PDF + email/fax |
| `Invoices/GenerateInvoice.asp` | `MyDeskASPNet/GenerateInvoice.aspx` | Invoice PDF + email |
| `Invoices/GenerateDeliveryNote.asp` | `MyDeskASPNet/GenerateDeliveryNote.aspx` | Delivery note PDF |
| `PurchaseOrders/GeneratePO.asp` | `MyDeskASPNet/GeneratePurchaseOrder.aspx` | PO PDF + email |
| `RFQ/GenerateRFQ.aspx` | (local .aspx) | RFQ PDF generation |

---

## Database

### Connection

**File**: `/System/ssi_dbConn_open_TL.inc`

```vbscript
MyDB = Server.MapPath("/Database") & "\" & "Techlight.mdb"
strConn = "Driver={Microsoft Access Driver (*.mdb)};DBQ=" & MyDB & ";"
Set dbConn = Server.CreateObject("ADODB.Connection")
dbConn.Open strConn
```

**Production Location**: `C:\Database\Techlight2.mdb`

### Key Tables (Inferred from Code)

| Table | Purpose |
|-------|---------|
| `Quotes` | Sales quotations (Qid, Status, Customer, Project, etc.) |
| `QuoteItems` | Line items within quotes |
| `QuoteStatus` | Status lookup (Draft, Issued, Approved, Declined) |
| `QuoteCOS` | Conditions of Sale |
| `Invoices` | Customer invoices |
| `InvoiceItems` | Invoice line items |
| `PurchaseOrders` | Supplier purchase orders |
| `POItems` | PO line items |
| `RFQ` / `RFQItems` | Request for quotation |
| `Contacts` | Customer contacts |
| `Companies` | Client organizations |
| `Users` | System users (employees) |
| `Divisions` | Business divisions/branches |
| `Locations` | Office/warehouse locations |
| `Products` | Product catalog |
| `PartCodes` | Product part codes |
| `UsersAccess` | User permissions per division |

---

## Security Model

### Authentication

1. **Login Form**: `/Clients/SalesEngine/Portal/Validate.asp`
2. **Validation**: Username + password checked against `Users` table
3. **Session State**: Stored in cookies (not server session)
   - `LoggedIn` (boolean)
   - `UserSettings` (Name, Code, Email, Manager status)
   - `DivisionIdsAccess` (authorized divisions)
   - `ClientSettings` (WorkingDir, Prefix, Stylesheet)

### Authorization

- **Division-Based Access**: Users have visibility/manager/quote/RFQ/PO permissions per division
- **Role Checking**: `Request.Cookies("DivisionIdsAccess")("Quotes") <> "0"`
- **Security Include**: `/System/ssi_Security.inc` checks `LoggedIn` cookie

### Cookie Structure

```javascript
// Primary authentication
LoggedIn = true/false

// User details
UserSettings: {
  Name, Code, Email, DivisionId,
  Manager (boolean),
  UserTypeId,
  LineManagerName, LineManagerEmail
}

// Division permissions  
DivisionIdsAccess: {
  Quotes, RFQ, PurchaseOrders, Payroll,
  ArrDivisionIds (visible),
  ArrDivisionIdsManager (manager rights)
}

// Client config
ClientSettings: {
  WorkingDir: "/Clients/SalesEngineTL",
  Prefix: "TL",
  Stylesheet: "TT_Style.css"
}
```

---

## Key Design Patterns

### 1. Page Naming Conventions

| Suffix | Purpose | Example |
|--------|---------|---------|
| `Default.asp` | List/grid view | `Quotes/Default.asp` |
| `View.asp` | Read-only detail | `Quotes/View.asp` |
| `Edit.asp` | Edit form | `Quotes/Edit.asp` |
| `Add.asp` | Create form | `Quotes/Add.asp` |
| `*_Proc.asp` | Form processor | `Quotes/Edit_Proc.asp` |
| `IFrame.asp` | Grid data iframe | `Quotes/IFrame.asp` |
| `NavBar.asp` | Action buttons | `Quotes/NavBar.asp` |
| `Email.asp` / `Fax.asp` | Communication forms | `Quotes/Email.asp` |
| `Report.asp` | Reporting interface | `Quotes/Report.asp` |

### 2. Cache Control Headers

Every page includes aggressive no-cache headers:
```vbscript
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
```

### 3. Common Includes Pattern

Standard page template:
```vbscript
<% ' Cache headers %>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
```

### 4. IFrame-Based Grids

Main page (`Default.asp`) → Contains iframe → Loads `IFrame.asp` with data grid
- Uses ActiveWidgets JavaScript grid library
- Server-side paging and sorting
- AJAX-like behavior via iframe reloading

### 5. URL Patterns

```
/Clients/SalesEngineTL/
├── Portal/
│   └── Validate.asp          # Login POST handler
├── Quotes/
│   ├── Default.asp           # Quote list page
│   ├── IFrame.asp            # Grid data (iframe src)
│   ├── View.asp?Qid=5806     # View specific quote
│   ├── Edit.asp?Qid=5806     # Edit specific quote
│   ├── Add.asp               # Create new quote
│   ├── Email.asp?Qid=5806    # Email quote
│   └── GenerateQuote.asp     # Redirects to .NET for PDF
└── Header.asp                # Navigation frame
```

---

## Site Map (User Flow)

### Login Flow
```
1. https://techlight.digitalresponse.com.au/
   └── Redirects to login form

2. /Clients/SalesEngineTL/?NoCache=...
   └── Login page (Username/Password)

3. POST to /Clients/SalesEngine/Portal/Validate.asp
   └── Sets cookies, redirects to PortalFrame.asp

4. /Clients/SalesEngineTL/PortalFrame.asp
   └── Main dashboard (frameset: Header + Content)
```

### Quote Management Flow
```
PortalFrame.asp (Home)
    └── Navigate to Quotes
        └── /Clients/SalesEngineTL/Quotes/Default.asp
            ├── View Quote List (with filters)
            ├── Click "Add Quote" 
            │   └── Add.asp → Add2.asp → Add_Proc.asp
            ├── Click "View" on row
            │   └── View.asp?Qid=5806
            │       ├── Print (PDF via .NET)
            │       ├── Email → Email.asp → GenerateQuote.asp → .NET → Email_Proc.asp
            │       └── Edit → Edit.asp → Edit_Proc.asp
            └── Click "Edit" on row
                └── Edit.asp → Edit_Proc.asp
```

---

## Packages & Components

### ASP.NET Dependencies (NuGet)

| Package | Version | Purpose |
|---------|---------|---------|
| EntityFramework | 6.0.0 | ORM (legacy) |
| jQuery | 3.5.1 | DOM manipulation |
| jQuery.UI | 1.8.20 | UI widgets |
| Microsoft.AspNet.Providers | 2.0.0 | Membership/roles |
| Newtonsoft.Json | 12.0.3 | JSON serialization |
| Modernizr | 2.8.3 | Browser feature detection |

### Third-Party Components

| Component | Purpose | Location |
|-----------|---------|----------|
| WebSupergoo.ABCpdf11 | PDF generation from HTML | Referenced in .NET project |
| ActiveWidgets | JavaScript data grid | `/System/aw.js`, `grid.js` |
| Calendar JS | Date picker | `/System/cal2.js` |

---

## System Settings & Configuration

### IIS Configuration

**ASP Classic App** (`/Clients/SalesEngineTL/`):
- ASP version: Classic ASP (VBScript)
- Application pool: Integrated mode
- 32-bit enabled (for Access DB)

**ASP.NET App** (`/MyDeskASPNet/`):
- Framework: .NET Framework 4.8
- Separate application pool
- Forms authentication enabled

### Database Path Configuration

Production: `C:\Database\Techlight2.mdb`
Development: `c:\Development\Techlight.digitalresponse.com.au\Database\Techlight.mdb`

### Client Prefix Mapping

| URL Contains | Prefix | Database | Working Dir |
|--------------|--------|----------|-------------|
| SalesEngineTL | TL | Techlight2.mdb | /Clients/SalesEngineTL |
| SalesEngineTT | TT | TTL2.mdb | /Clients/SalesEngineTT |
| SalesEngineVA | VA | Vantage.mdb | /Clients/SalesEngineVA |

---

## Development Notes

### Local Development

The system can run on localhost - the .NET handlers detect `SERVER_NAME` and adjust:
```csharp
string host = Request.ServerVariables["SERVER_NAME"];
if (host == "localhost") {
    host = "techlight.digitalresponse.com.au";  // For URL generation
}
```

### Working with IFrames

The UI relies heavily on iframes which can complicate debugging:
- Right-click in iframe → Inspect for that frame's content
- JavaScript: `parent.function()` to call parent frame functions
- Cookies maintain state across frames

### Database Changes

1. Access DB location: `C:\Database\Techlight2.mdb` (production)
2. Backup before schema changes
3. Connection uses Microsoft Access Driver (ODBC)

### PDF Generation

The PDF workflow:
1. User triggers action (Print/Email/Fax)
2. ASP Classic updates DB status
3. Redirects to `/MyDeskASPNet/Generate*.aspx`
4. .NET uses ABCpdf to scrape `View.asp?email=true`
5. PDF saved to `/Clients/SalesEngineTL/[Module]s/Files/`
6. Redirects back to ASP Classic for email/fax sending

### Known Challenges

1. **Framesets**: Navigation can be confusing due to frame-based architecture
2. **Browser Compatibility**: Designed for IE8-era browsers; modern browser testing required
3. **No Source Control Integration**: Direct file editing on server (historical practice)
4. **State Management**: Cookie-based (not session) - sensitive to browser cookie settings
5. **PDF Generation**: Relies on external ABCpdf component (licensed)

---

## File Modification Guidelines

### When Modifying ASP Classic Pages

1. **Always include security check**: `<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->`
2. **Add cache headers** at top of page
3. **Use `ServerToEST()`** for all date handling (timezone conversion)
4. **SQL Injection Protection**: Use `Replace(string, "'", "''")` for string inputs
5. **Close database connections**: Include `ssi_dbConn_close.inc` when done

### When Adding New Modules

1. Follow existing naming conventions (`Default.asp`, `View.asp`, etc.)
2. Create `IFrame.asp` for list grids
3. Add navigation to `Header.asp` dropdown
4. Update permission checks in `Validate.asp` if needed
5. Consider PDF generation needs (may need .NET handler)

### Testing Changes

1. Test on localhost first if possible
2. Clear browser cache (aggressive no-cache headers help)
3. Test authenticated and unauthenticated access
4. Verify database changes don't break existing queries

---

## Contact & Support

- **Production URL**: https://techlight.digitalresponse.com.au
- **Database**: Techlight2.mdb (MS Access)
- **Hosting**: IIS on Windows VM
- **Client**: Techlight (project lighting specialists)

---

*Document generated for AI-assisted development - April 2026*
