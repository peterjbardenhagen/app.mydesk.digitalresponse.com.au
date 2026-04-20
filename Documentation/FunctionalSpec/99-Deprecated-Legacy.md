# 99 — Deprecated and Legacy Components

Components, files, and patterns that are obsolete, deprecated, or retained for backward compatibility only.

---

## 1. Deprecated Files

### 1.1 Legacy Upgrade Scripts

| File | Location | Status | Notes |
|---|---|---|---|
| `Upgrade22Sept09.asp` | `/Setup/` | Legacy | Database upgrade from 2009 |
| `Cleanup-LegacyTables.sql` | `/Database/` | Maintenance | Removes obsolete tables |

### 1.2 Old Client Folders

| Folder | Status | Replacement |
|---|---|---|
| `/Clients/Techlight/` | Deprecated | `/Clients/SalesEngineTL/` |
| `/Clients/TL0039/` | Deprecated | `/Clients/SalesEngineTL/` |

### 1.3 Legacy System Files

| File | Replacement | Notes |
|---|---|---|
| `ssi_FunctionsOld.asp` | `ssi_Functions.asp` | Legacy function library |
| `Style_Old.css` | `Style_Techlight.css` | Legacy stylesheet |

---

## 2. Legacy Patterns

### 2.1 Deprecated URL Patterns

| Old Pattern | New Pattern | Status |
|---|---|---|
| `…/AddNew.asp` | `…/Add.asp` | Standardized |
| `…/Delete.asp` | `…/Del_Proc.asp` | Processor naming |
| `…/Save.asp` | `…/Add_Proc.asp`, `…/Edit_Proc.asp` | Split by action |

### 2.2 Legacy Database Fields

| Field | Table | Status | Replacement |
|---|---|---|---|
| `OldAddress` | Quotes | Deprecated | Structured address fields |
| `PhoneOld` | Contacts | Deprecated | `Phone` |
| `DateCreated` | Various | Deprecated | `DateEntered` |
| `UserID` | Various | Deprecated | `Code` |

### 2.3 Legacy Table Names

| Old Name | Current Name | Notes |
|---|---|---|
| `tblQuotes` | `Quotes` | Removed tbl_ prefix |
| `tblInvoices` | `Invoices` | Removed tbl_ prefix |
| `QuoteLineItems` | `QuoteContents` | Renamed for consistency |
| `InvoiceLineItems` | `InvoiceContents` | Renamed for consistency |

---

## 3. Obsolete Features

### 3.1 Removed Modules

| Module | Status | Replacement |
|---|---|---|
| `SalesOrders/` | Removed | Merged with Quotes |
| `Inventory/` | Removed | Planned for future |
| `Timesheets/` | Removed | External system |
| `Expenses/` | Removed | External system |

### 3.2 Deprecated Functions

| Function | File | Status | Replacement |
|---|---|---|---|
| `OldLoginCheck()` | `ssi_Security.asp` | Removed | `ssi_Security.inc` |
| `MyDateFormat()` | `ssi_Dates.inc` | Deprecated | `FormatDateU()` |
| `LegacyConnect()` | `ssi_dbConn.asp` | Removed | `ssi_dbConn_open.inc` |

---
## 4. Backward Compatibility

### 4.1 Legacy Cookie Names

| Old Cookie | New Cookie | Compatibility Layer |
|---|---|---|
| `UserID` | `UserSettings("Code")` | `ssi_LegacyCompat.asp` |
| `Division` | `UserSettings("DivisionId")` | `ssi_LegacyCompat.asp` |
| `WorkingFolder` | `ClientSettings("WorkingDir")` | `ssi_LegacyCompat.asp` |

### 4.2 Compatibility Includes

**`ssi_LegacyCompat.asp`**
```asp
' Provides backward compatibility for:
' - Old cookie names
' - Legacy variable names  
' - Deprecated function wrappers
```

---

## 5. Technical Debt

### 5.1 Known Legacy Issues

| Issue | Location | Severity | Mitigation |
|---|---|---|---|
| MD5 Passwords | `Users` table | High | Migrate to bcrypt |
| Inline SQL | Various | Medium | Parameterized queries |
| On Error Resume Next | Various | Low | Structured error handling |
| Denormalized State | Address tables | Low | Normalize in future |
| Hardcoded Paths | Various | Medium | Config file extraction |

### 5.2 Deprecated Libraries

| Library | Replacement | Status |
|---|---|---|
| ASPHTTP | Native `Server.CreateObject("MSXML2.ServerXMLHTTP")` | Removed |
| ASPImage | .NET interop or direct GDI | Deprecated |
| CDONTS | CDO.Message | Removed |

---

## 6. Planned Deprecations

### 6.1 Upcoming Changes (as noted in code)

| Component | Target Removal | Replacement |
|---|---|---|
| `WorkingDir` cookie reliance | Future | Explicit parameter passing |
| `DivisionIdsAccess` string format | Future | JSON or separate cookies |
| Legacy `*.asp` extensions | Future | `*.aspx` migration |
| Inline styles | Future | Full CSS classes |
| Table-based layouts | Future | Flexbox/Grid |

### 6.2 Migration Notes in Source

```asp
' From various files:
' TODO: Replace with parameterized query
' TODO: Remove legacy cookie fallback
' TODO: Migrate to .NET handler
' TODO: Update to modern CSS
```

---

## 7. Retained for Compatibility

### 7.1 Active Legacy Support

| Feature | Why Retained | Migration Path |
|---|---|---|
| `ssi_LegacyCompat.asp` | Old bookmarks/cookies | Keep indefinitely |
| `DateEntered` + `EnteredBy` | Audit requirements | Keep, enhance |
| `Chr(39)` for quotes | Existing data | Maintain for now |
| `ServerToEST()` | Timezone handling | Enhance, don't replace |

### 7.2 Deprecated but Functional

| Feature | Status | Notes |
|---|---|---|
| `Request.Cookies("OldName")` | Redirected | Compatibility layer active |
| Legacy print styles | Maintained | `@media print` enhanced |
| IE-specific CSS | Minimal | Dropped in Modern.css |

---

## 8. Cleanup Recommendations

### 8.1 Safe to Remove

| Item | Verification Steps |
|---|---|
| `Upgrade22Sept09.asp` | Confirm no 2009-era databases exist |
| Old client folders | Verify no active users/links |
| `Style_Old.css` | Check all pages use new styles |
| Legacy upgrade scripts | Archive before deletion |

### 8.2 Requires Migration Before Removal

| Item | Migration Required |
|---|---|
| MD5 passwords | User password reset cycle |
| Hardcoded SendGrid key | Move to config/API key table |
| Inline SQL | Full code review and testing |
| `On Error Resume Next` | Structured error handling implementation |

---

## 9. Version History

### Major Transitions

| Version | Era | Key Changes |
|---|---|---|
| Pre-2009 | Legacy | `tbl_` prefixes, MD5 passwords |
| 2009-2015 | Classic ASP | Consolidated structure |
| 2015-2020 | Hybrid | ASP.NET interop added |
| 2020-2024 | Modernization | New UI, hardened code |
| Future | .NET Core | Planned full migration |

---

## 10. Related Documentation

- **01-Architecture.md** — Current architecture
- **04-Shared-System-Includes.md** — Current includes
- **91-File-Inventory.md** — Complete file list
