# MyDesk Granular Permission System

**Version:** 3.1  
**Last Updated:** May 2026

---

## Overview

MyDesk now implements a comprehensive **granular permission system** that controls access to every module and function based on User Types (roles). All future MyDesk code must use this system for authorization.

---

## Architecture

### Components

1. **PermissionService** (`src/MyDesk.Shared/Services/PermissionService.cs`)
   - Central service managing all permissions
   - Loads/saves permissions to `RolePermissions` database table
   - Caches permission sets for performance
   - Provides default permissions for standard roles

2. **PermissionModels** (`src/MyDesk.Shared/Models/PermissionModels.cs`)
   - `PermissionDefinition`: Defines a single permission (module.action)
   - `RolePermission`: Database model for storing permissions
   - `UserPermissionSet`: Cached permission set for a user type
   - `PermissionModule`: Groups permissions by module

3. **UserRolesAdmin Page** (`src/MyDesk.Web/Components/Pages/Admin/UserRolesAdmin.razor`)
   - UI for configuring granular permissions per role
   - Tabbed interface showing each user type
   - Matrix of modules and actions with checkboxes
   - Bulk select/deselect per module
   - "Apply Defaults" button to reset to standard permissions

4. **Database Table**: `RolePermissions`
   ```sql
   CREATE TABLE RolePermissions (
       RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
       UserTypeId INT NOT NULL,
       PermissionKey NVARCHAR(100) NOT NULL,
       IsAllowed BIT NOT NULL DEFAULT 1,
       CreatedAt DATETIME DEFAULT GETDATE(),
       UpdatedAt DATETIME NULL,
       FOREIGN KEY (UserTypeId) REFERENCES UserRoles(UserTypeId)
   );
   ```

---

## Permission Format

Permissions follow the format: `module.action`

Examples:
- `quotes.view` - View quotes
- `quotes.add` - Create quotes
- `quotes.edit` - Edit quotes
- `quotes.delete` - Delete quotes
- `quotes.pdf` - Generate PDF
- `quotes.email` - Email quotes
- `admin_users.manage_admins` - Manage Administrator users

---

## Default Role Permissions

### Director (UserTypeId = 1)
- **Full access to everything EXCEPT:**
  - ❌ `admin_users.manage_admins` - Cannot add, edit, view, or delete Administrator users
- All other permissions: ✅ Granted

### Administrator (UserTypeId = 2)
- **Full access to everything:**
  - ✅ All modules and actions
  - ✅ Can manage all user types including Directors and other Administrators

### Accounts (UserTypeId = 3)
- ✅ Dashboard, Invoices, Purchase Orders, Despatch, Reconciliation, Accounting
- ✅ Reports, Contacts, Companies, Files, Settings, Profile
- ✅ Activity, Favourites, Calendar, Expenses, Noticeboard
- ❌ Cannot delete invoices
- ❌ No access to: Marketing, Ask AI, Admin setup, User management

### Sales (UserTypeId = 4)
- ✅ Dashboard, Quotes, Contacts, Companies
- ✅ Files, Settings, Profile, Activity, Favourites, Calendar
- ✅ Noticeboard, Ask AI
- ❌ No access to: Invoices, POs, Accounting, Admin features

---

## How to Check Permissions in Code

### In Razor Pages

```csharp
@inject PermissionService PermSvc

@code {
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }
    private int _userTypeId;
    
    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask != null)
        {
            var auth = await AuthStateTask;
            var userTypeClaim = auth.User.FindFirst("UserTypeId")?.Value;
            if (int.TryParse(userTypeClaim, out var utid))
                _userTypeId = utid;
        }
    }
    
    // Check permission
    private async Task<bool> CanViewQuotes() => 
        await PermSvc.HasPermissionAsync(_userTypeId, "quotes", "view");
}
```

### In Services

```csharp
public class QuoteService
{
    private readonly PermissionService _permSvc;
    
    public async Task<Quote?> GetQuoteAsync(int userTypeId, int quoteId)
    {
        if (!await _permSvc.HasPermissionAsync(userTypeId, "quotes", "view"))
            throw new UnauthorizedAccessException("You do not have permission to view quotes");
        
        // ... rest of logic
    }
}
```

---

## Director Restriction on Administrators

Directors **cannot**:
- View Administrator users in the user list
- Edit Administrator user details
- Delete or deactivate Administrator users
- Create new Administrator users (role not available in dropdown)

This is enforced at multiple levels:
1. **UsersList.razor**: Filters out Administrator users from the list
2. **UserEdit.razor**: Redirects if Director tries to edit an Administrator
3. **PermissionService.CanManageUserType()**: Returns `false` for Director→Administrator

---

## Available Permissions by Module

### Dashboard
- `dashboard.view` - Access the main dashboard

### Quotes
- `quotes.view`, `quotes.add`, `quotes.edit`, `quotes.delete`
- `quotes.pdf`, `quotes.email`, `quotes.convert`, `quotes.copy`

### Invoices
- `invoices.view`, `invoices.add`, `invoices.edit`, `invoices.delete`
- `invoices.pdf`, `invoices.email`, `invoices.export_myob`, `invoices.despatch`

### Purchase Orders
- `purchase_orders.view`, `purchase_orders.add`, `purchase_orders.edit`, `purchase_orders.delete`
- `purchase_orders.pdf`, `purchase_orders.email`, `purchase_orders.invoice_details`

### Despatch
- `despatch.view`, `despatch.edit`

### Job Orders
- `job_orders.view`, `job_orders.add`, `job_orders.edit`, `job_orders.delete`

### Contacts (CRM)
- `contacts.view`, `contacts.add`, `contacts.edit`, `contacts.delete`
- `contacts.import`, `contacts.export`

### Companies (CRM)
- `companies.view`, `companies.add`, `companies.edit`, `companies.delete`
- `companies.import`

### Products
- `products.view`, `products.add`, `products.edit`, `products.delete`

### Reports
- `reports.view`, `reports.export`

### Reconciliation
- `reconciliation.view`, `reconciliation.sync_myob`, `reconciliation.aged_payables`

### Accounting
- `accounting.view`

### Marketing
- `marketing.view`, `marketing.strategy`, `marketing.ai`, `marketing.campaigns`
- `marketing.cdp`, `marketing.sdp`, `marketing.brand_assets`

### Ask AI
- `ask_ai.use`

### Files
- `files.view`, `files.upload`, `files.delete`

### Admin - Users
- `admin_users.view`, `admin_users.add`, `admin_users.edit`, `admin_users.delete`
- `admin_users.manage_roles`, `admin_users.manage_admins`

### Admin - System Setup
- `admin_setup.view`, `admin_setup.divisions`, `admin_setup.locations`
- `admin_setup.quote_status`, `admin_setup.invoice_status`, `admin_setup.po_status`
- `admin_setup.job_order_status`, `admin_setup.parameters`, `admin_setup.currency`
- `admin_setup.part_codes`, `admin_setup.activity_types`

### Admin - Platform
- `admin_platform.view`, `admin_platform.edit`, `admin_platform.branding`
- `admin_platform.nav_menu`, `admin_platform.setup_menu`

### Admin - Logs & Audit
- `admin_logs.view`, `admin_logs.purge`, `admin_logs.ai_audit`

### Admin - User Roles & Permissions
- `admin_user_roles.view`, `admin_user_roles.edit`, `admin_user_roles.manage_permissions`

### Integrations
- `integrations.view`, `integrations.configure`

### Settings
- `settings.view`, `settings.edit_preferences`, `settings.change_password`

### Other
- `noticeboard.view`, `calendar.view`, `expenses.view/add/edit/delete`
- `activity.view`, `favourites.view`, `profile.view/edit`

---

## Managing Permissions

### Via UI
1. Navigate to **Admin > User Roles & Permissions** (`/admin/user-roles`)
2. Select a role tab (Director, Administrator, Accounts, Sales, etc.)
3. Check/uncheck permissions for each module
4. Click **Save Permissions**
5. Use **Apply Defaults** to reset to standard permissions

### Via Code
```csharp
// Save single permission
await permSvc.SavePermissionAsync(userTypeId, "quotes.edit", true);

// Save all permissions for a role
await permSvc.SaveAllPermissionsAsync(userTypeId, new Dictionary<string, bool>
{
    { "quotes.view", true },
    { "quotes.edit", true },
    // ... etc
});

// Apply defaults
await permSvc.ApplyDefaultPermissionsAsync();
```

---

## Security Best Practices

1. **Always check permissions** before allowing actions, not just for UI visibility
2. **Use PermissionService** rather than hardcoded role checks
3. **Director restriction** is enforced at the service layer - do not bypass
4. **Cache invalidation** happens automatically when permissions are saved
5. **Database table** is created automatically on first startup

---

## Future Development

**ALL future MyDesk code MUST:**
1. Define permissions for new modules/actions in `PermissionService.InitializePermissions()`
2. Check permissions using `PermSvc.HasPermissionAsync()` before allowing operations
3. Add UI controls only if user has the relevant permission
4. Document new permissions in this file

Example for a new module:
```csharp
// In PermissionService.InitializePermissions()
AddPerm("newmodule", "view", "View New Module", "Access new module");
AddPerm("newmodule", "add", "Add New Module", "Create new items");
AddPerm("newmodule", "edit", "Edit New Module", "Edit existing items");
AddPerm("newmodule", "delete", "Delete New Module", "Delete items");
```

---

## Troubleshooting

### Permissions not showing in UI
- Check that `RolePermissions` table exists (created automatically on startup)
- Click "Apply Defaults" to populate initial permissions
- Check logs for database connection errors

### Permission changes not taking effect
- Permissions are cached - they refresh automatically when saved
- Force reload: Navigate away and back to the page, or restart the app

### Director can still see Administrators
- Clear browser cache
- Check that `_currentUserTypeId` is correctly set to 1 (Director)
- Verify `CanManageUserType()` is being called

---

**Powered by Digital Response**  
**© 2026 Digital Response. All rights reserved.**
