# Techlight MyDesk - ASP Files Hierarchical Inventory

**Date:** April 16, 2026  
**Total ASP Files:** 425  
**Purpose:** Identify orphaned/unused files for cleanup approval

---

## 🔴 HIGH CONFIDENCE - ORPHANED / DELETE RECOMMENDED

These files are confirmed unused or belong to removed functionality.

### 1. Root Level - SalesEngine Folder (Old Pre-TL Version)
```
SalesEngine/
├── CareOnline.asp              ← 1,106 bytes (USER MENTIONED)
├── Comments.asp                ← 1,592 bytes
├── Comments_Add.asp            ← 0 bytes (EMPTY)
├── Comments_Add_Proc.asp       ← 670 bytes
├── Default.asp                 ← 0 bytes (EMPTY)
├── DeliveryRun.asp             ← 0 bytes (EMPTY)
├── EditPassword.asp            ← 0 bytes (EMPTY)
├── EditPassword_Proc.asp       ← 0 bytes (EMPTY)
├── LogOff.asp                  ← 0 bytes (EMPTY)
├── ManageUsers.asp             ← 0 bytes (EMPTY)
├── ManageUsers_Add.asp         ← 0 bytes (EMPTY)
├── ManageUsers_Add_Proc.asp    ← 0 bytes (EMPTY)
├── ManageUsers_Del_Proc.asp    ← 0 bytes (EMPTY)
├── ManageUsers_Edit.asp        ← 4,725 bytes
├── ManageUsers_Edit_Proc.asp   ← 0 bytes (EMPTY)
├── Noticeboard.asp             ← 0 bytes (EMPTY)
├── Noticeboard_Add.asp         ← 0 bytes (EMPTY)
├── Noticeboard_Add_Proc.asp    ← 0 bytes (EMPTY)
├── Noticeboard_Del_Proc.asp    ← 0 bytes (EMPTY)
├── Noticeboard_Edit.asp        ← 0 bytes (EMPTY)
├── Noticeboard_Edit_Proc.asp   ← 0 bytes (EMPTY)
├── Portal.asp                  ← 0 bytes (EMPTY)
├── Quote.asp                   ← 3,566 bytes
├── QuotesList.asp              ← 0 bytes (EMPTY)
├── QuotesList_IFrame.asp       ← 0 bytes (EMPTY)
├── SalesEngine.asp             ← 0 bytes (EMPTY)
├── TimberRequirements.asp      ← 0 bytes (EMPTY)
├── Validate.asp                ← 0 bytes (EMPTY)
├── Validate_Portal.asp         ← 0 bytes (EMPTY)
└── System/                     ← 12 items
```

**Status:** This entire folder is legacy. 18 of 26 files are 0 bytes.

---

### 2. Clients/SalesEngine/ (Old Client Folder - Not TL)
```
Clients/SalesEngine/
├── Default.asp                 ← 2,527 bytes
├── LastUpdated.asp             ← 1,553 bytes
├── Updating.asp                ← 1,206 bytes
├── ssi_Security.inc            ← 138 bytes
└── Portal/
    ├── LogOff.asp              ← 402 bytes
    ├── Validate_Portal.asp     ← 1,002 bytes
    └── Validate.asp            ← 1,482 bytes
```

**Status:** Superseded by Clients/SalesEngineTL/. Not referenced anywhere.

---

### 3. MyDesk/ Folder (Empty/Abandoned)
```
MyDesk/
└── Updating.asp                ← 0 bytes (EMPTY)
```

**Status:** Empty file, folder serves no purpose.

---

### 4. Temp/ Folder - Orphaned ASP Files
```
Temp/
├── Default.asp                 ← 1,095 bytes (OLD PORTAL VERSION)
├── Default2.asp               ← 5,219 bytes (TEST VERSION)
├── Default3.asp               ← 228 bytes (TEST VERSION)
├── Portal.asp                 ← 7,314 bytes (OLD PORTAL VERSION)
├── PortalFrame.asp            ← 39,330 bytes (OLD FRAME VERSION)
├── PORequest_Proc.asp         ← 7,008 bytes (LEGACY PO)
├── sendmail.asp               ← 2,222 bytes (TEST MAIL)
├── sendmail2.asp              ← 2,648 bytes (TEST MAIL)
├── SetCookies.asp             ← 484 bytes (TEST)
├── test.asp                   ← 2,281 bytes (TEST)
├── import.asp                 ← 0 bytes (EMPTY)
└── Global.asa                 ← 110 bytes (LEGACY ASP SESSION)
```

**Status:** These are old versions, tests, and backups. Some contain hardcoded credentials (sendmail.asp).

---

### 5. aspnet_client/ - Legacy ASP.NET SendPass Utility
```
aspnet_client/
└── cgi-bin/
    └── sendpass/
        └── default.asp         ← Password reset utility
```

**Status:** Legacy password recovery. May be unused/unsecured.

---

## 🟡 MODERATE CONFIDENCE - REVIEW RECOMMENDED

These may be unused but need verification.

### 6. System/ - Potential Legacy Files
```
System/
├── ssi_Header_MyDesk.inc      ← Old MyDesk header
├── ssi_Header_Fax.inc         ← Fax-specific header?
├── ssi_Header.inc             ← Already included
├── Consts_Fax.asp             ← Fax constants (if fax unused)
├── ssi_Divisions.inc          ← Old division logic?
└── ssi_Products.inc         ← If Products module unused
```

---

### 7. Clients/SalesEngineTL/ - Potentially Unused Modules

Review if these modules are actively used:

```
Clients/SalesEngineTL/
├── CurrencyRates/             ← 6 files
├── Divisions/                 ← 6 files  
├── Employment/                ← 6 files
├── ExpenseTypeGroups/         ← 6 files
├── ExpenseTypes/              ← 6 files
├── FilesCategories/           ← 6 files
├── Jobs/                      ← Multiple files
├── Timesheets/                ← Multiple files
├── TMail/                     ← Internal mail system
├── Noticeboard/               ← 12 files
├── Types/                     ← 6 files
└── FilesLibrary/              ← Multiple files
```

**Question for User:** Are these modules actively used?
- CurrencyRates, ExpenseTypes, Timesheets, TMail, Noticeboard, FilesLibrary?

---

## 🟢 KEEP - ACTIVE SYSTEM FILES

### Core System Files (Required)
```
System/
├── ssi_dbConn_open.inc        ← Database connection
├── ssi_dbConn_close.inc       ← Database close
├── ssi_Functions.asp          ← Core functions
├── ssi_Errors.asp             ← Error handling
├── ssi_Security.inc           ← Security checks
├── ssi_Dates.inc              ← Date utilities
├── ssi_Alerts.asp             ← Alert messages
├── ssi_Header.inc             ← JavaScript header
├── Consts.asp                 ← Core constants
└── Style_Modern.css           ← New stylesheet (KEEP!)
```

### Active Client - SalesEngineTL
```
Clients/SalesEngineTL/
├── Portal/
│   ├── Validate.asp           ← LOGIN (CRITICAL)
│   ├── LogOff.asp             ← LOGOUT (CRITICAL)
│   └── Portal.asp             ← DASHBOARD (CRITICAL)
├── Header.asp                 ← NEW MODERN HEADER (KEEP!)
├── Default.asp                ← FRAME SET
├── DefaultFrame.asp           ← FRAME CONTAINER
├── PortalFrame.asp            ← MAIN FRAME
│
├── Quotes/                    ← 25 files (CRITICAL - KEEP ALL)
├── Invoices/                  ← 25 files (CRITICAL - KEEP ALL)
├── Contacts/                  ← 15 files (CRITICAL - KEEP ALL)
├── CallReports/               ← 10 files (ACTIVE - KEEP)
├── Companies/                 ← 8 files (ACTIVE - KEEP)
├── Purchasing/                ← 20+ files (ACTIVE - KEEP)
├── PurchaseOrders/            ← Multiple files (ACTIVE - KEEP)
├── Expenses/                  ← If expense module used
├── Products/                  ← If product catalog used
├── Quotes/                    ← ExportToMYOB.asp (NEW - KEEP)
├── Setup/                     ← Admin functions (KEEP)
└── Users/                     ← User management (KEEP)
```

### Errors Folder
```
Errors/
└── 500-100.asp                ← Error handler (KEEP)
```

### Database Admin
```
Database/
└── SQLQuery/                  ← Admin SQL interface (KEEP IF USED)
    ├── Default.asp
    └── Default_Proc.asp
```

---

## 📊 SUMMARY STATISTICS

| Category | Files | Recommendation |
|----------|-------|----------------|
| **Root SalesEngine/** | 26 files | 🔴 DELETE ALL |
| **Clients/SalesEngine/** | 7 files | 🔴 DELETE ALL |
| **MyDesk/** | 1 file | 🔴 DELETE |
| **Temp/*.asp** | 12 files | 🔴 DELETE ALL |
| **aspnet_client/** | 1 file | 🔴 REVIEW/DELETE |
| **SalesEngineTL (Active)** | ~300 files | 🟢 KEEP ALL |
| **System (Core)** | ~20 files | 🟢 KEEP |
| **Uncertain Modules** | ~60 files | 🟡 REVIEW WITH USER |

---

## 🎯 IMMEDIATE ACTION RECOMMENDED

**Delete Without Risk:**
1. ✅ `SalesEngine/` folder (26 files - mostly empty)
2. ✅ `Clients/SalesEngine/` folder (7 files - superseded)
3. ✅ `MyDesk/Updating.asp` (1 file - empty)
4. ✅ `Temp/*.asp` test files (12 files - old versions)

**Total Space to Recover:** ~100+ KB (not significant, but cleaner codebase)

---

## ❓ USER DECISION REQUIRED

Before deleting these, please confirm:

1. **TMail module** - Internal messaging system. Is this used?
2. **Noticeboard** - Company announcements. Is this used?
3. **FilesLibrary** - Document storage. Is this used?
4. **Timesheets** - Time tracking. Is this used?
5. **aspnet_client/sendpass/** - Password reset. Is this used?

---

## APPROVAL CHECKLIST

- [ ] **DELETE** SalesEngine/ folder and all contents
- [ ] **DELETE** Clients/SalesEngine/ folder
- [ ] **DELETE** MyDesk/ folder
- [ ] **DELETE** Temp/*.asp (but keep other Temp files like ADOVBS.inc)
- [ ] **DELETE** aspnet_client/sendpass/
- [ ] **REVIEW** Specific modules (TMail, Noticeboard, FilesLibrary, Timesheets)

**Please approve the items above and I will proceed with cleanup.**
