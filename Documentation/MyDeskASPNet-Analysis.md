# MyDeskASPNet Folder Analysis

**Date:** April 16, 2026  
**Status:** ⚠️ Build requires Visual Studio/MSBuild (legacy ASP.NET Web Forms project)  
**Build Status:** Cannot build with `dotnet build` - requires full MSBuild/Visual Studio

---

## 🎯 CRITICAL FINDING: All Components Are ACTIVE

**DO NOT DELETE** - This folder contains essential PDF generation functionality used by the main ASP Classic application.

---

## 📁 Folder Structure

```
MyDeskASPNet/
├── .vs/                        ← Visual Studio cache (can clean)
├── bin/                        ← Build output (empty - needs rebuild)
├── obj/                        ← Build temp (empty - needs rebuild)
├── Clients/
│   └── SalesEngineTL/
│       └── Quotes/             ← Copy of Quote PDF (legacy)
├── Content/
│   └── themes/
│       └── base/               ← jQuery UI CSS + images
├── packages/                   ← NuGet packages (142 items)
├── Properties/
│   └── AssemblyInfo.cs         ← Assembly metadata
├── Scripts/
│   ├── jquery-3.5.1.*          ← jQuery library
│   ├── jquery-ui-1.8.20.*      ← jQuery UI library
│   └── modernizr-2.8.3.js      ← Modernizr
├── .csproj / .sln              ← Project files
├── Web.config                  ← Configuration
└── **ACTIVE ASPX PAGES**       ← See below
```

---

## 🔴 CRITICAL - ACTIVE ASPX PAGES (DO NOT DELETE)

These pages are actively called by the Classic ASP application:

### 1. **ScrapeToPDF.aspx** ⭐ CORE COMPONENT
- **Purpose:** Converts ASP pages to PDF using ABCpdf library
- **Usage:** Called by all Generate* pages
- **Dependencies:** ABCpdf.dll (licensed PDF library)
- **Called by:** GenerateQuote.aspx, GenerateInvoice.aspx, etc.
- **Status:** CRITICAL - CANNOT DELETE

### 2. **GenerateQuote.aspx** ⭐ CRITICAL
- **Purpose:** Generates Quote PDFs for email/fax
- **Usage:** Called from Quotes module
- **References Found:**
  - `Clients/SalesEngineTL/Quotes/Email.asp`
  - `Clients/SalesEngineTL/Quotes/Fax.asp`
  - `Clients/SalesEngineTL/Quotes/GenerateQuote.asp`
  - `Clients/SalesEngineTL/JobOrders/GenerateQuote.asp`
  - `System/Global.js`
- **Status:** CRITICAL - CANNOT DELETE

### 3. **GenerateInvoice.aspx** ⭐ CRITICAL
- **Purpose:** Generates Invoice PDFs
- **Usage:** Called from Invoices module
- **References Found:**
  - `Clients/SalesEngineTL/Invoices/Email.asp`
  - `Clients/SalesEngineTL/Invoices/Fax.asp`
  - `Clients/SalesEngineTL/Invoices/GenerateInvoice.asp`
- **Status:** CRITICAL - CANNOT DELETE

### 4. **GenerateDeliveryNote.aspx** ⭐ CRITICAL
- **Purpose:** Generates Delivery Note PDFs
- **Usage:** Called from Invoices module
- **References Found:**
  - `Clients/SalesEngineTL/Invoices/GenerateDeliveryNote.asp`
  - `Clients/SalesEngineTL/Invoices/EmailDeliveryNote.asp`
  - `Clients/SalesEngineTL/Invoices/GenerateDeliveryNote.aspx.vb`
- **Status:** CRITICAL - CANNOT DELETE

### 5. **GeneratePurchaseOrder.aspx** ⭐ CRITICAL
- **Purpose:** Generates Purchase Order PDFs
- **Usage:** Called from Purchasing module
- **References Found:**
  - `Clients/SalesEngineTL/PurchaseOrders/GeneratePO.asp`
- **Status:** CRITICAL - CANNOT DELETE

### 6. **MakeThumbnails.aspx** ⚠️ REVIEW NEEDED
- **Purpose:** Creates thumbnails from PDFs in FilesLibrary
- **Current Config:** Points to `SalesEngineTT` (old client)
- **Issue:** Hardcoded path to `C:\inetpub\Websites\mydesk.com.au_2.0\Clients\SalesEngineTT\FilesLibrary\Files`
- **Status:** Likely ORPHANED - references wrong client folder
- **Recommendation:** Verify if file thumbnails are needed

### 7. **UnitTests.aspx** ⚠️ DEVELOPMENT ONLY
- **Purpose:** Test page for PDF generation
- **Usage:** Development/testing only
- **Status:** NOT USED IN PRODUCTION
- **Recommendation:** Can be deleted after verification

---

## 📦 NuGet Packages (Required)

Key dependencies:
- **ABCpdf** v11.3.0.0 - PDF generation (licensed)
- **Newtonsoft.Json** v12.0.3 - JSON handling
- **EntityFramework** v6.0.0 - Database ORM
- **jQuery** v3.5.1 - Frontend JS
- **jQuery.UI** v1.8.20 - UI components

---

## 🔧 Build Configuration

**Issue:** Current project uses legacy ASP.NET Web Forms
- **Target Framework:** .NET Framework 4.8
- **Build Tool:** Requires MSBuild (not compatible with `dotnet build`)
- **Visual Studio:** Requires VS 2019/2022 or Build Tools

**Error:**
```
error MSB4019: The imported project "...\WebApplications\Microsoft.WebApplication.targets" was not found
```

---

## 🧹 Safe Cleanup (With Approval)

### Can Delete (After Verification):
| File | Reason |
|------|--------|
| `UnitTests.aspx` | Test page only |
| `MakeThumbnails.aspx` | References old client (SalesEngineTT) |
| `.vs/` folder | IDE cache |
| `Clients/SalesEngineTL/Quotes/` copy | Appears to be duplicate |

### Can Clean:
| Folder | Action |
|--------|--------|
| `bin/` | Clean build output |
| `obj/` | Clean temp files |
| `packages/` | Consider migrating to PackageReference |

---

## ✅ RECOMMENDATIONS

### Immediate:
1. ✅ **KEEP ALL** Generate*.aspx files - actively used
2. ✅ **KEEP** ScrapeToPDF.aspx - core PDF engine
3. ✅ **KEEP** All NuGet packages - required for build
4. ⚠️ **VERIFY** MakeThumbnails.aspx usage
5. ⚠️ **DELETE** UnitTests.aspx (dev only)

### Future:
1. Consider migrating to .NET 6+ Web API for PDF generation
2. Move from Web Forms to modern ASP.NET Core
3. Update ABCpdf license if needed

---

## 🚫 DO NOT DELETE

These are actively called by the production system:

```
ScrapeToPDF.aspx
GenerateQuote.aspx
GenerateInvoice.aspx  
GenerateDeliveryNote.aspx
GeneratePurchaseOrder.aspx
Web.config
packages.config
All .cs files
All DLLs in bin/
```

---

## Summary

**MyDeskASPNet is CRITICAL infrastructure** for PDF generation in the Techlight MyDesk system. The .NET components handle:
- Quote PDF generation and emailing
- Invoice PDF generation and emailing
- Delivery Note PDF generation
- Purchase Order PDF generation

**Without this folder, quotes and invoices cannot be generated or emailed to customers.**

---

**Action Required:**
- [ ] Verify MakeThumbnails.aspx is unused
- [ ] Delete UnitTests.aspx (dev only)
- [ ] Clean .vs/, bin/, obj/ folders
- [ ] Set up proper build environment (Visual Studio 2019+)
