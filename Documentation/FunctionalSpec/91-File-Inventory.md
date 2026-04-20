# 91 — File Inventory

Complete inventory of all application files by module and purpose.

---

## 1. Root Level Files

| File | Type | Size | Purpose |
|---|---|---|---|
| `index.asp` | ASP | ~1 KB | Root redirect |
| `Portal.asp` | ASP | ~5 KB | Main entry/login |
| `PortalFrame.asp` | ASP | ~8 KB | Application frame |
| `Dashboard.asp` | ASP | ~12 KB | Post-login dashboard |
| `Timezone.asp` | ASP | ~2 KB | Timezone utilities |
| `favicon.ico` | ICO | ~1 KB | Site icon |

---

## 2. System Folder (`/System/`)

### 2.1 Security & Core

| File | Type | Purpose |
|---|---|---|
| `ssi_Security.inc` | INC | Login/session validation |
| `ssi_ResponseHeaders.inc` | INC | HTTP header management |
| `ssi_dbConn_open.inc` | INC | Database connection open |
| `ssi_dbConn_close.inc` | INC | Database connection close |
| `ssi_LegacyCompat.asp` | ASP | Backward compatibility |

### 2.2 Functions & Utilities

| File | Type | Purpose |
|---|---|---|
| `ssi_Functions.asp` | ASP | Master functions include |
| `ssi_Functions_Core.asp` | ASP | Core utility functions |
| `ssi_Functions_User.asp` | ASP | User-related functions |
| `ssi_Functions_Quote.asp` | ASP | Quote helper functions |
| `ssi_Functions_PO.asp` | ASP | PO helper functions |
| `ssi_Functions_UI.asp` | ASP | UI helper functions |
| `ssi_Functions_Activity.asp` | ASP | Activity tracking |
| `ssi_Functions_Files.asp` | ASP | File operations |
| `ssi_Errors.asp` | ASP | Error handling |
| `ssi_Alerts.asp` | ASP | Alert/notification system |
| `ssi_SafeExecute.inc` | INC | Safe execution wrapper |

### 2.3 Data & Configuration

| File | Type | Purpose |
|---|---|---|
| `Var.asp` | ASP | Variable declarations |
| `Constants.asp` | ASP | Global constants |
| `ssi_Dates.inc` | INC | Date/time utilities |

### 2.4 Stylesheets

| File | Type | Purpose |
|---|---|---|
| `Style_Techlight.css` | CSS | Main application styles |
| `Style_Modern.css` | CSS | Modern UI components |
| `Style2.css` | CSS | Legacy styles |
| `grid.css` | CSS | Grid/table styles |
| `Quotes.css` | CSS | Quote-specific styles |
| `PurchaseOrders.css` | CSS | PO-specific styles |
| `Style_Print.css` | CSS | Print media styles |

### 2.5 JavaScript

| File | Type | Purpose |
|---|---|---|
| `Global.js` | JS | Core JavaScript utilities |
| `Quotes.js` | JS | Quote form functions |
| `JobOrders.js` | JS | Job order functions |
| `PurchaseOrders.js` | JS | PO form functions |
| `grid.js` | JS | ActiveWidgets grid control |
| `paging1.js` | JS | Pagination control |
| `cal2.js` | JS | Calendar date picker |
| `cal_conf2.js` | JS | Calendar configuration |

---

## 3. Client Module: Quotes (`/Clients/SalesEngineTL/Quotes/`)

### Core Pages
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~35 KB |
| `IFrame.asp` | ASP | ~45 KB |
| `Add.asp` | ASP | ~21 KB |
| `Add2.asp` | ASP | ~26 KB |
| `Add_Proc.asp` | ASP | ~8 KB |
| `Edit.asp` | ASP | ~30 KB |
| `Edit_Proc.asp` | ASP | ~11 KB |
| `View.asp` | ASP | ~33 KB |
| `NavBar.asp` | ASP | ~7 KB |
| `NavBarDeliveryNote.asp` | ASP | ~4 KB |

### Actions
| File | Type | Size |
|---|---|---|
| `Email.asp` | ASP | ~7 KB |
| `Email_Proc.asp` | ASP | ~11 KB |
| `Print.asp` | ASP | ~4 KB |
| `Print_Proc.asp` | ASP | ~3 KB |
| `UpdateStatus.asp` | ASP | ~6 KB |
| `UpdateStatus_Proc.asp` | ASP | ~3 KB |
| `Del_Proc.asp` | ASP | ~4 KB |

### PDF & Documents
| File | Type | Size |
|---|---|---|
| `GenerateQuote.asp` | ASP | ~3 KB |
| `GenerateQuote.aspx` | ASPX | ~2 KB |
| `GenerateQuote.aspx.vb` | VB | ~7 KB |

### Despatch & Delivery
| File | Type | Size |
|---|---|---|
| `EnterDespatchDetails.asp` | ASP | ~11 KB |
| `EnterDespatchDetails_Proc.asp` | ASP | ~4 KB |
| `ViewDespatchNote.asp` | ASP | ~15 KB |
| `ViewDeliveryNote.asp` | ASP | ~12 KB |
| `EmailDeliveryNote.asp` | ASP | ~6 KB |
| `EmailDeliveryNote_Proc.asp` | ASP | ~12 KB |

### Transporters
| File | Type | Size |
|---|---|---|
| `Transporter_QuoteToJob.asp` | ASP | ~3 KB |
| `Transporter_QuoteToInvoice.asp` | ASP | ~3 KB |

### Reports
| File | Type | Size |
|---|---|---|
| `Report.asp` | ASP | ~19 KB |
| `ViewHistory.asp` | ASP | ~9 KB |

### Supporting
| File | Type | Size |
|---|---|---|
| `Files/` | DIR | PDF output directory |
| `Images/` | DIR | (if exists) |

---

## 4. Client Module: Invoices (`/Clients/SalesEngineTL/Invoices/`)

### Core Pages
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~33 KB |
| `IFrame.asp` | ASP | ~26 KB |
| `Add.asp` | ASP | ~27 KB |
| `Add_Proc.asp` | ASP | ~14 KB |
| `Edit.asp` | ASP | ~30 KB |
| `Edit_Proc.asp` | ASP | ~11 KB |
| `View.asp` | ASP | ~28 KB |
| `NavBar.asp` | ASP | ~8 KB |
| `NavBarDeliveryNote.asp` | ASP | ~5 KB |

### Actions
| File | Type | Size |
|---|---|---|
| `Email.asp` | ASP | ~7 KB |
| `Email_Proc.asp` | ASP | ~11 KB |
| `UpdateStatus.asp` | ASP | ~6 KB |
| `UpdateStatus_Proc.asp` | ASP | ~2 KB |
| `Del_Proc.asp` | ASP | ~3 KB |

### PDF & Documents
| File | Type | Size |
|---|---|---|
| `GenerateInvoice.asp` | ASP | ~3 KB |
| `GenerateDeliveryNote.asp` | ASP | ~3 KB |

### MYOB & Data Entry
| File | Type | Size |
|---|---|---|
| `EnterMYOBDetails.asp` | ASP | ~9 KB |
| `EnterMYOBDetails_Proc.asp` | ASP | ~5 KB |

### Reports
| File | Type | Size |
|---|---|---|
| `Report.asp` | ASP | ~16 KB |
| `ViewHistory.asp` | ASP | ~8 KB |

### Supporting
| File | Type | Size |
|---|---|---|
| `Files/` | DIR | PDF output |
| `Despatch/` | DIR | (if exists) |

---

## 5. Client Module: Purchase Orders (`/Clients/SalesEngineTL/PurchaseOrders/`)

### Core Pages
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~35 KB |
| `IFrame.asp` | ASP | ~26 KB |
| `Add.asp` | ASP | ~8 KB |
| `Add2.asp` | ASP | ~24 KB |
| `Add_Proc.asp` | ASP | ~9 KB |
| `Edit.asp` | ASP | ~23 KB |
| `Edit_Proc.asp` | ASP | ~7 KB |
| `View.asp` | ASP | ~28 KB |
| `ViewRequest.asp` | ASP | ~21 KB |
| `NavBar.asp` | ASP | ~7 KB |
| `NavBar_Requests.asp` | ASP | ~3 KB |

### Actions
| File | Type | Size |
|---|---|---|
| `Approve.asp` | ASP | ~5 KB |
| `Decline.asp` | ASP | ~4 KB |
| `Email.asp` | ASP | ~7 KB |
| `Email_Proc.asp` | ASP | ~10 KB |
| `UpdateStatus.asp` | ASP | ~9 KB |
| `UpdateStatus_Proc.asp` | ASP | ~4 KB |
| `Del_Proc.asp` | ASP | ~5 KB |

### PDF & Documents
| File | Type | Size |
|---|---|---|
| `GeneratePO.asp` | ASP | ~3 KB |
| `GeneratePO.aspx` | ASPX | ~2 KB |
| `GeneratePO.aspx.vb` | VB | ~7 KB |

### Invoice Details
| File | Type | Size |
|---|---|---|
| `EnterInvoiceDetails.asp` | ASP | ~10 KB |
| `EnterInvoiceDetails_Proc.asp` | ASP | ~5 KB |

### RFQ & Generation
| File | Type | Size |
|---|---|---|
| `GenerateFromRFQ.asp` | ASP | ~4 KB |

### Reports
| File | Type | Size |
|---|---|---|
| `Report.asp` | ASP | ~15 KB |
| `ViewHistory.asp` | ASP | ~12 KB |

### Supporting
| File | Type | Size |
|---|---|---|
| `Files/` | DIR | PDF output |

---

## 6. Client Module: Job Orders (`/Clients/SalesEngineTL/JobOrders/`)

### Core Pages
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~16 KB |
| `IFrame.asp` | ASP | ~42 KB |
| `Add.asp` | ASP | ~26 KB |
| `Add_Proc.asp` | ASP | ~12 KB |
| `Edit.asp` | ASP | ~12 KB |
| `Edit_Proc.asp` | ASP | ~4 KB |
| `EditJobOrder.asp` | ASP | ~15 KB |
| `EditJobOrder_Proc.asp` | ASP | ~5 KB |
| `View.asp` | ASP | ~20 KB |
| `NavBar.asp` | ASP | ~2 KB |

### Actions
| File | Type | Size |
|---|---|---|
| `Email.asp` | ASP | ~7 KB |
| `Email_Proc.asp` | ASP | ~10 KB |
| `UpdateStatus.asp` | ASP | ~7 KB |
| `UpdateStatus_Proc.asp` | ASP | ~3 KB |
| `Del_Proc.asp` | ASP | ~4 KB |

### PDF & Transporters
| File | Type | Size |
|---|---|---|
| `GenerateQuote.asp` | ASP | ~3 KB |
| `GenerateQuote.aspx` | ASPX | ~2 KB |
| `GenerateQuote.aspx.vb` | VB | ~6 KB |
| `Transporter.asp` | ASP | ~2 KB |

### Reports
| File | Type | Size |
|---|---|---|
| `Report.asp` | ASP | ~15 KB |
| `ViewHistory.asp` | ASP | ~9 KB |

---

## 7. CRM Modules

### Contacts (`/Clients/SalesEngineTL/Contacts/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~16 KB |
| `Add.asp` | ASP | ~20 KB |
| `AddNewWin.asp` | ASP | ~19 KB |
| `Edit.asp` | ASP | ~22 KB |
| `View.asp` | ASP | ~11 KB |
| `DeliveryAddress.asp` | ASP | ~21 KB |
| `InvoiceAddress.asp` | ASP | ~21 KB |
| `Report.asp` | ASP | ~12 KB |
| `IFrame.asp` | ASP | ~7 KB |

### Companies (`/Clients/SalesEngineTL/Companies/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~9 KB |
| `Add.asp` | ASP | ~7 KB |
| `Edit.asp` | ASP | ~7 KB |
| `IFrame.asp` | ASP | ~9 KB |

---

## 8. Product Management

### Products (`/Clients/SalesEngineTL/Products/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~6 KB |
| `Default2.asp` | ASP | ~7 KB |
| `Add.asp` | ASP | ~12 KB |
| `Edit.asp` | ASP | ~14 KB |
| `Select.asp` | ASP | ~6 KB |
| `IFrame.asp` | ASP | ~7 KB |

### PartCodes (`/Clients/SalesEngineTL/PartCodes/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~6 KB |
| `Add.asp` | ASP | ~6 KB |
| `Edit.asp` | ASP | ~7 KB |

### Projects (`/Clients/SalesEngineTL/Projects/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~6 KB |
| `Add.asp` | ASP | ~6 KB |
| `Edit.asp` | ASP | ~6 KB |

---

## 9. Administration

### Users (`/Clients/SalesEngineTL/Users/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~14 KB |
| `Add.asp` | ASP | ~42 KB |
| `Edit.asp` | ASP | ~46 KB |
| `EditPassword.asp` | ASP | ~6 KB |
| `Hierachy.asp` | ASP | ~7 KB |

### Divisions (`/Clients/SalesEngineTL/Divisions/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~8 KB |
| `Add.asp` | ASP | ~10 KB |
| `Edit.asp` | ASP | ~11 KB |

### Locations (`/Clients/SalesEngineTL/Locations/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~7 KB |
| `Add.asp` | ASP | ~17 KB |
| `Edit.asp` | ASP | ~19 KB |

---

## 10. Setup & Configuration

### Setup Hub (`/Clients/SalesEngineTL/Setup/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~53 KB |
| `Maintenance.asp` | ASP | ~3 KB |
| `Script.asp` | ASP | ~2 KB |
| `CreateTableHistory.asp` | ASP | ~2 KB |
| `APIKeys/` | DIR | (empty) |

### Parameters (`/Clients/SalesEngineTL/Parameters/`)
| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~7 KB |
| `Edit_Proc.asp` | ASP | ~2 KB |

### TableComments (`/Clients/SalesEngineTL/TableComments/`)
| File | Type | Size |
|---|---|---|
| `Comments.asp` | ASP | ~6 KB |
| `Add.asp` | ASP | ~8 KB |
| `Add_Proc.asp` | ASP | ~4 KB |
| `IFrame.asp` | ASP | ~11 KB |
| `ViewRecord.asp` | ASP | ~3 KB |

---

## 11. Reports (`/Clients/SalesEngineTL/Reports/`)

| File | Type | Size |
|---|---|---|
| `Default.asp` | ASP | ~15 KB |
| `SalesReportGen.asp` | ASP | ~7 KB |
| `SalesReport.asp` | ASP | ~65 KB |
| `SalesReport_All.asp` | ASP | ~39 KB |
| `SalesReport_Data1-7.asp` | ASP | ~3 KB each |
| `PurchaseOrders_ByMonth_ByDivision.asp` | ASP | ~11 KB |
| `Chart.asp` | ASP | ~4 KB |
| `Upload.asp` | ASP | ~5 KB |

---

## 12. Portal & Access Control (`/Clients/SalesEngineTL/Portal/`)

| File | Type | Size |
|---|---|---|
| `AccessDenied.asp` | ASP | ~3 KB |
| `Validate.asp` | ASP | ~5 KB |
| `Validate_Portal.asp` | ASP | ~5 KB |

---

## 13. ASP.NET Interop (`/MyDeskASPNet/`)

| File | Type | Size |
|---|---|---|
| `GenerateQuote.aspx` | ASPX | ~2 KB |
| `GenerateQuote.aspx.cs` | C# | ~9 KB |
| `GenerateInvoice.aspx` | ASPX | ~2 KB |
| `GenerateInvoice.aspx.cs` | C# | ~7 KB |
| `GeneratePurchaseOrder.aspx` | ASPX | ~2 KB |
| `GeneratePurchaseOrder.aspx.cs` | C# | ~7 KB |
| `GenerateDeliveryNote.aspx` | ASPX | ~2 KB |
| `GenerateDeliveryNote.aspx.cs` | C# | ~7 KB |
| `ScrapeToPDF.aspx` | ASPX | ~2 KB |
| `ScrapeToPDF.aspx.cs` | C# | ~6 KB |
| `Web.config` | XML | ~9 KB |
| `MyDeskASPNet.csproj` | XML | ~32 KB |
| `MyDeskASPNet.sln` | SLN | ~2 KB |

---

## 14. Images (`/Images/`)

| File | Type | Purpose |
|---|---|---|
| `Calendar.gif` | GIF | Calendar picker icon |
| `Spacer.gif` | GIF | Layout spacer |
| `Logo*.gif/png` | IMG | Division logos |
| `Icon*.gif` | GIF | UI icons |

---

## 15. Summary Statistics

| Category | Files | Approx Lines |
|---|---|---|
| Core System | 20+ | ~5,000 |
| Quotes Module | 35+ | ~15,000 |
| Invoices Module | 25+ | ~12,000 |
| Purchase Orders | 30+ | ~14,000 |
| Job Orders | 20+ | ~10,000 |
| CRM (Contacts/Companies) | 15+ | ~8,000 |
| Products/PartCodes | 12+ | ~5,000 |
| Administration | 20+ | ~12,000 |
| Setup/Config | 10+ | ~8,000 |
| Reports | 12+ | ~10,000 |
| ASP.NET Interop | 10+ | ~3,000 |
| **TOTAL** | **~200+** | **~100,000+** |

---

## 16. File Naming Conventions

### Suffix Patterns

| Suffix | Purpose | Example |
|---|---|---|
| `_Proc.asp` | Form processor | `Add_Proc.asp` |
| `_Win.asp` | Popup window | `AddNewWin.asp` |
| `NavBar.asp` | Navigation bar | `NavBar.asp` |
| `IFrame.asp` | Grid content | `IFrame.asp` |
| `Default.asp` | Module entry | `Default.asp` |
| `View.asp` | Read-only view | `View.asp` |
| `Edit.asp` | Edit form | `Edit.asp` |

---

## 17. Directory Structure Summary

```
/
├── Portal.asp, Dashboard.asp
├── System/
│   ├── ssi_*.inc, ssi_*.asp
│   ├── *.css, *.js
│   └── Constants.asp, Var.asp
├── Clients/
│   └── SalesEngineTL/
│       ├── Dashboard.asp, Portal.asp
│       ├── Quotes/, Invoices/, PurchaseOrders/
│       ├── JobOrders/
│       ├── Contacts/, Companies/
│       ├── Products/, PartCodes/, Projects/
│       ├── Users/, Divisions/, Locations/
│       ├── Setup/, Parameters/, TableComments/
│       ├── Reports/
│       └── Portal/
├── MyDeskASPNet/
│   ├── Generate*.aspx, *.cs
│   └── Web.config
└── Images/
```
