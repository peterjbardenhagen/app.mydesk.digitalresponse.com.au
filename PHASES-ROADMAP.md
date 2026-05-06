# Feature Implementation Roadmap

## Status: Phase 1 Complete ✅

### Phase 1: Contact/Company Address Refactoring ✅ COMPLETE
**Commit:** `e8a6681`
- ✅ Removed Address fields from Contact model (Address1, Address2, Suburb, State, PostCode)
- ✅ Created migration 020_RemoveContactAddresses.sql 
- ✅ Updated ContactService to remove address field references
- ✅ Updated ContactView.razor to show company Invoice/Delivery addresses
- ✅ Updated ContactEdit.razor to guide users to edit company addresses
- ✅ Removed address fields from vCard export

**Next Step:** Before starting Phase 2, run the migration:
```sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Techlight_MyDesk" -i "src\Deployment\Migration\020_RemoveContactAddresses.sql"
```

---

## Phase 2: Purchase Order Improvements (HIGH PRIORITY)

### 2.1: Fix Division Display
- **Issue:** Division field shows `0` instead of division name
- **Location:** `src/MyDesk.Web/Components/Pages/PurchaseOrders/PurchaseOrderEdit.razor` (line ~60)
- **Fix:** Change PropertyColumn from `x => x.DivisionId` display to `x => x.DivisionName` or use TemplateColumn to display selected division name
- **Files:** PurchaseOrderEdit.razor

### 2.2: Add Payment Types Dropdown
- **Issue:** Payment Types dropdown only shows "none"
- **Location:** `src/MyDesk.Web/Components/Pages/PurchaseOrders/PurchaseOrderEdit.razor`
- **Fix:** Add hardcoded payment types:
  - Account Supplier
  - Bank Transfer
  - Cash on Delivery
  - Cheque
  - Credit Application
  - Credit Card
- **Default:** Select "Account Supplier" by default
- **Files:** PurchaseOrderEdit.razor

### 2.3: Conditional Delivery Address Display
- **Issue:** Delivery Address should only show if "Deliver to Location" is NOT "*** SPECIFIED ADDRESS"
- **Fix:** Add conditional display: `@if (model.DeliverToLocation != "*** SPECIFIED ADDRESS")`
- **Files:** PurchaseOrderEdit.razor

### 2.4: Auto-populate Delivery Address from Company
- **Issue:** Delivery Address should auto-fill from Contact's Company Delivery Address
- **Fix:** 
  1. When Contact is selected, fetch its Company
  2. Populate DeliverToAddress with Company.DeliveryAddress
  3. Only show address editor if "Deliver to Location" = "*** SPECIFIED ADDRESS"
- **Files:** PurchaseOrderEdit.razor, PurchaseOrderService.cs

### 2.5: Auto-populate from Location
- **Issue:** If Deliver to Location is selected from a location dropdown, populate address from Location
- **Fix:** Create LocationsService to fetch location addresses, populate DeliverToAddress when location is selected
- **Files:** PurchaseOrderEdit.razor, PurchaseOrderService.cs, LocationsService.cs

---

## Phase 3: Tenant Isolation for Locations & Divisions (HIGH PRIORITY)

### 3.1: Add TenantId to Locations
- **Files:** 
  - Create migration: `021_AddTenantIdToLocations.sql`
  - Update Location model in DomainModels.cs
  - Update LocationsService.cs to filter by TenantId
  - Update /admin/locations page to isolate by tenant
- **Details:**
  - Add `TenantId UNIQUEIDENTIFIER` column
  - Add FK to Tenants table
  - TenantIsolationService will auto-add RLS

### 3.2: Add TenantId to Divisions
- **Files:**
  - Create migration: `022_AddTenantIdToDivisions.sql`
  - Update Division model in DomainModels.cs
  - Update DivisionService.cs to filter by TenantId
  - Update PO/Invoice/Quote UI to show tenant-specific divisions
- **Details:**
  - Add `TenantId UNIQUEIDENTIFIER` column
  - Add FK to Tenants table
  - TenantIsolationService will auto-add RLS

### 3.3: Fix /admin/locations Page
- **Location:** `src/MyDesk.Web/Components/Pages/Admin/Locations.razor`
- **Changes:**
  - Add address fields (like Companies): Address1, Address2, Suburb, State, PostCode
  - Update Add Location dialog to include address fields
  - Use same address editor UI as Companies page
  - Ensure locations are tenant-isolated

---

## Phase 4: Super Administrator Module (CRITICAL)

### 4.1: Create SuperAdmin Role & Authorization
- **Files:**
  - Update PermissionModels.cs to add "SuperAdmin" role
  - Create migration to add role to database
  - Update AuthService.cs to check for SuperAdmin role
  - Create [Authorize(Roles = "SuperAdmin")] attribute class
- **Security:** Only super admins can access platform-wide settings

### 4.2: Build SuperAdmin Dashboard
- **Path:** `/admin/super-admin` (protected by SuperAdmin role)
- **Files:** Create new page `src/MyDesk.Web/Components/Pages/Admin/SuperAdmin.razor`
- **Layout:** Tab-based interface
  - **Tab 1: Tenant Management**
    - List all tenants (CRUD operations)
    - Add tenant: form with TenantId, TenantName, Slug, Subscription Plan, Max Users, Storage Limit
    - Edit tenant: update all fields
    - Delete tenant: soft-delete with warning
  - **Tab 2: Technical Configuration**
    - View current appsettings (read-only display)
    - Show environment variables
    - Show database connection status
    - Show mail server configuration (masked)
  - **Tab 3: Log Management**
    - View application & error logs from database
    - Filter by date range
    - Filter by log level (Info, Warning, Error, Fatal)
    - Show retention policy settings
    - Dropdown: Select log retention days (1, 7, 30, 365)
    - Button: "Apply Retention Policy" to run cleanup

### 4.3: Move Log Viewer to SuperAdmin
- **Current:** `src/MyDesk.Web/Components/Pages/Admin/LogViewer.razor`
- **Action:** Move to SuperAdmin module as Tab 3
- **Changes:**
  - Update route to `/admin/super-admin` (LogViewer becomes internal to this page)
  - Add role protection: SuperAdmin only

### 4.4: Implement Database Logging
- **Files:**
  - Create table in migration: `023_CreateApplicationLogTable.sql`
    ```sql
    CREATE TABLE ApplicationLogs (
        LogId BIGINT PRIMARY KEY IDENTITY(1,1),
        Timestamp DATETIME2 DEFAULT GETDATE(),
        Level VARCHAR(20), -- Info, Warning, Error, Fatal
        Category VARCHAR(256),
        Message NVARCHAR(MAX),
        Exception NVARCHAR(MAX),
        TenantId UNIQUEIDENTIFIER,
        UserId INT NULL,
        RequestPath VARCHAR(2000),
        StatusCode INT NULL
    )
    ```
  - Create LoggingService.cs to write logs to database
  - Update Program.cs to use database logging provider
  - Implement Serilog sink for SQL Server (Serilog.Sinks.MSSqlServer)

### 4.5: Add Log Retention Policy
- **Files:**
  - Create migration: `024_CreateLogRetentionTable.sql`
    ```sql
    CREATE TABLE LogRetentionSettings (
        SettingId INT PRIMARY KEY,
        RetentionDays INT DEFAULT 30, -- 1, 7, 30, or 365
        LastCleanupTime DATETIME2
    )
    ```
  - Create LogRetentionService.cs
    - Method: `GetRetentionDaysAsync()` - reads setting from DB
    - Method: `SetRetentionDaysAsync(int days)` - updates setting
    - Method: `CleanupOldLogsAsync()` - deletes logs older than retention period
  - Update SuperAdmin page to call SetRetentionDaysAsync() when dropdown changes
  - Create scheduled task in Hangfire: `CleanupOldLogsAsync()` daily

### 4.6: SuperAdmin Navigation
- **Files:** `src/MyDesk.Web/Components/Layout/NavMenu.razor`
- **Changes:**
  - Add "Super Administrator" menu item (only shows if user is SuperAdmin)
  - Href="/admin/super-admin"
  - Icon: `Icons.Material.Rounded.Security`

---

## Update Quotes/Invoices/Despatch/JobOrders (Phase 2)

Once Phase 3 is complete, update these services to use correct address types:

### Quotes
- Use Company.InvoiceAddress (billing address)
- Location: `src/MyDesk.Web/Components/Pages/Quotes/QuoteView.razor`

### Invoices
- Use Company.InvoiceAddress (billing address)
- Location: `src/MyDesk.Web/Components/Pages/Invoices/InvoiceView.razor`

### Despatch
- Use Company.DeliveryAddress (shipping address)
- Location: `src/MyDesk.Web/Components/Pages/Despatch/DespatchList.razor`

### Job Orders
- Use Company.DeliveryAddress (work location)
- Location: `src/MyDesk.Web/Components/Pages/JobOrders/JobOrderView.razor`

---

## Estimated Effort

| Phase | Complexity | Time Estimate | Priority |
|-------|-----------|---------------|----------|
| Phase 1 | Medium | 30 mins | ✅ DONE |
| Phase 2 | Medium | 2-3 hours | HIGH |
| Phase 3 | High | 3-4 hours | HIGH |
| Phase 4 | Very High | 4-6 hours | CRITICAL |

**Total Estimated Effort:** ~10-15 hours

## Testing Checklist

After each phase:
- [ ] Run `dotnet build` - 0 errors, 0 warnings
- [ ] Run migrations in order
- [ ] Test Kestrel: `http://localhost:5237/login`
- [ ] Manual testing in browser
- [ ] Run Playwright tests: `dotnet test tests/MyDesk.PlaywrightTests`

---

**Last Updated:** May 7, 2026
**Status:** Phase 1 ✅ Complete | Phase 2-4 Pending
