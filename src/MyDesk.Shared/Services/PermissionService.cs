using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Manages granular permissions for user types. All future MyDesk code should use this service
/// to check permissions rather than relying on role-based [Authorize] attributes alone.
/// </summary>
public class PermissionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PermissionService> _logger;
    private readonly ConcurrentDictionary<string, PermissionDefinition> _allPermissions = new();
    private ConcurrentDictionary<int, UserPermissionSet>? _cachedPermissionSets;

    public PermissionService(IServiceScopeFactory scopeFactory, ILogger<PermissionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        InitializePermissions();
    }

    /// <summary>
    /// Define all available permissions in the system
    /// </summary>
    private void InitializePermissions()
    {
        // Dashboard
        AddPerm("dashboard", "view", "View Dashboard", "Access the main dashboard");
        
        // Quotes
        AddPerm("quotes", "view", "View Quotes", "View quote list and details");
        AddPerm("quotes", "add", "Add Quotes", "Create new quotes");
        AddPerm("quotes", "edit", "Edit Quotes", "Edit existing quotes");
        AddPerm("quotes", "delete", "Delete Quotes", "Delete quotes");
        AddPerm("quotes", "pdf", "Generate PDF", "Generate quote PDF");
        AddPerm("quotes", "email", "Email Quotes", "Email quotes to customers");
        AddPerm("quotes", "convert", "Convert to Invoice", "Convert quotes to invoices");
        AddPerm("quotes", "copy", "Copy Quote", "Copy supplier quotes");
        
        // Invoices
        AddPerm("invoices", "view", "View Invoices", "View invoice list and details");
        AddPerm("invoices", "add", "Add Invoices", "Create new invoices");
        AddPerm("invoices", "edit", "Edit Invoices", "Edit existing invoices");
        AddPerm("invoices", "delete", "Delete Invoices", "Delete invoices");
        AddPerm("invoices", "pdf", "Generate PDF", "Generate invoice PDF");
        AddPerm("invoices", "email", "Email Invoices", "Email invoices to customers");
        AddPerm("invoices", "export_myob", "Export to MYOB", "Export invoices to MYOB");
        AddPerm("invoices", "despatch", "Manage Despatch", "Create/edit despatch details");
        
        // Purchase Orders
        AddPerm("purchase_orders", "view", "View POs", "View purchase order list and details");
        AddPerm("purchase_orders", "add", "Add POs", "Create new purchase orders");
        AddPerm("purchase_orders", "edit", "Edit POs", "Edit existing purchase orders");
        AddPerm("purchase_orders", "delete", "Delete POs", "Delete purchase orders");
        AddPerm("purchase_orders", "pdf", "Generate PDF", "Generate PO PDF");
        AddPerm("purchase_orders", "email", "Email POs", "Email POs to suppliers");
        AddPerm("purchase_orders", "invoice_details", "Manage PO Invoices", "Manage PO invoice details");
        
        // Despatch
        AddPerm("despatch", "view", "View Despatch", "View despatch list and details");
        AddPerm("despatch", "edit", "Edit Despatch", "Edit despatch details");
        
        // Job Orders
        AddPerm("job_orders", "view", "View Job Orders", "View job order list and details");
        AddPerm("job_orders", "add", "Add Job Orders", "Create new job orders");
        AddPerm("job_orders", "edit", "Edit Job Orders", "Edit existing job orders");
        AddPerm("job_orders", "delete", "Delete Job Orders", "Delete job orders");
        
        // Contacts (CRM)
        AddPerm("contacts", "view", "View Contacts", "View contact list and details");
        AddPerm("contacts", "add", "Add Contacts", "Create new contacts");
        AddPerm("contacts", "edit", "Edit Contacts", "Edit existing contacts");
        AddPerm("contacts", "delete", "Delete Contacts", "Delete contacts");
        AddPerm("contacts", "import", "Import Contacts", "Import contacts from file");
        AddPerm("contacts", "export", "Export Contacts", "Export contacts");
        
        // Companies (CRM)
        AddPerm("companies", "view", "View Companies", "View company list and details");
        AddPerm("companies", "add", "Add Companies", "Create new companies");
        AddPerm("companies", "edit", "Edit Companies", "Edit existing companies");
        AddPerm("companies", "delete", "Delete Companies", "Delete companies");
        AddPerm("companies", "import", "Import Companies", "Import companies from file");
        
        // Products
        AddPerm("products", "view", "View Products", "View product list and details");
        AddPerm("products", "add", "Add Products", "Create new products");
        AddPerm("products", "edit", "Edit Products", "Edit existing products");
        AddPerm("products", "delete", "Delete Products", "Delete products");
        
        // Reports
        AddPerm("reports", "view", "View Reports", "Access reports");
        AddPerm("reports", "export", "Export Reports", "Export report data");
        
        // Reconciliation
        AddPerm("reconciliation", "view", "View Reconciliation", "View reconciliation dashboard");
        AddPerm("reconciliation", "sync_myob", "Sync MYOB", "Trigger MYOB sync");
        AddPerm("reconciliation", "aged_payables", "View Aged Payables", "View aged payables");
        
        // Accounting
        AddPerm("accounting", "view", "View Accounting", "Access accounting page");
        
        // Marketing
        AddPerm("marketing", "view", "View Marketing", "Access marketing hub");
        AddPerm("marketing", "strategy", "Manage Strategy", "Manage marketing strategy");
        AddPerm("marketing", "ai", "Use Marketing AI", "Use AI marketing tools");
        AddPerm("marketing", "campaigns", "Manage Campaigns", "Create/edit email campaigns");
        AddPerm("marketing", "cdp", "View CDP", "View Customer Data Platform");
        AddPerm("marketing", "sdp", "View SDP", "View Supplier Data Platform");
        AddPerm("marketing", "brand_assets", "Manage Brand Assets", "Upload/manage brand assets");
        
        // Ask AI
        AddPerm("ask_ai", "use", "Use Ask AI", "Use AI assistant");
        
        // Files
        AddPerm("files", "view", "View Files", "Access files library");
        AddPerm("files", "upload", "Upload Files", "Upload files");
        AddPerm("files", "delete", "Delete Files", "Delete files");
        
        // Admin - Users
        AddPerm("admin_users", "view", "View Users", "View user list");
        AddPerm("admin_users", "add", "Add Users", "Create new users");
        AddPerm("admin_users", "edit", "Edit Users", "Edit user details");
        AddPerm("admin_users", "delete", "Delete Users", "Delete users");
        AddPerm("admin_users", "manage_roles", "Manage User Roles", "Assign/change user roles");
        AddPerm("admin_users", "manage_admins", "Manage Administrators", "Add/edit/delete Administrator users (Directors cannot)");
        
        // Admin - System Setup
        AddPerm("admin_setup", "view", "View Setup", "Access setup home");
        AddPerm("admin_setup", "divisions", "Manage Divisions", "Manage divisions");
        AddPerm("admin_setup", "locations", "Manage Locations", "Manage locations");
        AddPerm("admin_setup", "quote_status", "Manage Quote Status", "Manage quote statuses");
        AddPerm("admin_setup", "invoice_status", "Manage Invoice Status", "Manage invoice statuses");
        AddPerm("admin_setup", "po_status", "Manage PO Status", "Manage PO statuses");
        AddPerm("admin_setup", "job_order_status", "Manage Job Order Status", "Manage job order statuses");
        AddPerm("admin_setup", "parameters", "Manage Parameters", "Manage system parameters");
        AddPerm("admin_setup", "currency", "Manage Currency", "Manage currency rates");
        AddPerm("admin_setup", "part_codes", "Manage Part Codes", "Manage part codes");
        AddPerm("admin_setup", "activity_types", "Manage Activity Types", "Manage activity types");
        
        // Admin - Platform
        AddPerm("admin_platform", "view", "View Platform Settings", "Access platform settings");
        AddPerm("admin_platform", "edit", "Edit Platform Settings", "Edit platform settings");
        AddPerm("admin_platform", "branding", "Manage Branding", "Manage platform branding");
        AddPerm("admin_platform", "nav_menu", "Manage Nav Menu", "Configure navigation menu");
        AddPerm("admin_platform", "setup_menu", "Manage Setup Menu", "Configure setup menu");
        
        // Admin - Logs & Audit
        AddPerm("admin_logs", "view", "View Logs", "View application logs");
        AddPerm("admin_logs", "purge", "Purge Logs", "Purge old logs");
        AddPerm("admin_logs", "ai_audit", "View AI Audit", "View AI interaction audit log");
        
        // Admin - User Roles & Permissions
        AddPerm("admin_user_roles", "view", "View User Roles", "View role definitions");
        AddPerm("admin_user_roles", "edit", "Edit User Roles", "Edit role definitions");
        AddPerm("admin_user_roles", "manage_permissions", "Manage Permissions", "Configure granular permissions per role");
        
        // Integrations
        AddPerm("integrations", "view", "View Integrations", "View integrations page");
        AddPerm("integrations", "configure", "Configure Integrations", "Configure integration settings");
        
        // Settings (user preferences)
        AddPerm("settings", "view", "View Settings", "Access account settings");
        AddPerm("settings", "edit_preferences", "Edit Preferences", "Edit personal preferences");
        AddPerm("settings", "change_password", "Change Password", "Change own password");
        
        // Noticeboard
        AddPerm("noticeboard", "view", "View Noticeboard", "View noticeboard");
        
        // Calendar
        AddPerm("calendar", "view", "View Calendar", "Access calendar");

        // Emails
        AddPerm("emails", "view", "View Emails", "Access Outlook inbox and email summaries");
        
        // Expenses
        AddPerm("expenses", "view", "View Expenses", "View expenses");
        AddPerm("expenses", "add", "Add Expenses", "Create expenses");
        AddPerm("expenses", "edit", "Edit Expenses", "Edit expenses");
        AddPerm("expenses", "delete", "Delete Expenses", "Delete expenses");
        
        // Activity
        AddPerm("activity", "view", "View Activity", "View recent activity feed");
        
        // Favourites
        AddPerm("favourites", "view", "View Favourites", "Access favourites");
        
        // Profile
        AddPerm("profile", "view", "View Profile", "View own profile");
        AddPerm("profile", "edit", "Edit Profile", "Edit own profile");
    }

    private void AddPerm(string module, string action, string displayName, string description)
    {
        var perm = new PermissionDefinition
        {
            Module = module,
            Action = action,
            DisplayName = displayName,
            Description = description
        };
        _allPermissions[perm.FullKey] = perm;
    }

    /// <summary>
    /// Get all defined permissions
    /// </summary>
    public IReadOnlyDictionary<string, PermissionDefinition> GetAllPermissions() => _allPermissions;

    /// <summary>
    /// Get permissions grouped by module
    /// </summary>
    public List<PermissionModule> GetPermissionsByModule()
    {
        var modules = _allPermissions.Values
            .GroupBy(p => p.Module)
            .Select(g => new PermissionModule
            {
                Key = g.Key,
                DisplayName = GetModuleDisplayName(g.Key),
                Icon = GetModuleIcon(g.Key),
                Actions = g.OrderBy(p => p.Action).ToList()
            })
            .OrderBy(m => m.DisplayName)
            .ToList();
        return modules;
    }

    private string GetModuleDisplayName(string module)
    {
        return string.Join(" ", module.Replace("_", " ").Split(' ')
            .Select(w => char.ToUpper(w[0]) + w[1..]));
    }

    private string GetModuleIcon(string module)
    {
        return module switch
        {
            "dashboard" => "Dashboard",
            "quotes" => "Description",
            "invoices" => "Receipt",
            "purchase_orders" => "ShoppingBasket",
            "despatch" => "LocalShipping",
            "job_orders" => "Work",
            "contacts" => "Person",
            "companies" => "Business",
            "products" => "Category",
            "reports" => "BarChart",
            "reconciliation" => "AccountBalance",
            "accounting" => "AccountBalance",
            "marketing" => "Campaign",
            "ask_ai" => "AutoAwesome",
            "files" => "Folder",
            "admin_users" => "People",
            "admin_setup" => "Tune",
            "admin_platform" => "Settings",
            "admin_logs" => "Description",
            "admin_user_roles" => "VerifiedUser",
            "integrations" => "Hub",
            "settings" => "Settings",
            "noticeboard" => "Campaign",
            "calendar" => "CalendarToday",
            "emails" => "Mail",
            "expenses" => "Payment",
            "activity" => "History",
            "favourites" => "Star",
            "profile" => "AccountCircle",
            _ => "Folder"
        };
    }

    /// <summary>
    /// Load all permissions from database for all user types
    /// </summary>
    public async Task<Dictionary<int, UserPermissionSet>> LoadAllPermissionsAsync()
    {
        var result = new ConcurrentDictionary<int, UserPermissionSet>();
        
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            var userTypes = await db.QueryAsync(
                @"SELECT UserTypeId, UserType AS UserRole FROM UserTypes ORDER BY UserTypeId");
            
            foreach (System.Data.DataRow row in userTypes.Rows)
            {
                var userTypeId = Convert.ToInt32(row["UserTypeId"]);
                var roleName = row["UserRole"]?.ToString() ?? string.Empty;
                
                var permSet = new UserPermissionSet
                {
                    UserTypeId = userTypeId,
                    RoleName = roleName
                };
                
                var perms = await db.QueryAsync(
                    @"SELECT PermissionKey, IsAllowed FROM RolePermissions WHERE UserTypeId = @UserTypeId",
                    new() { ["UserTypeId"] = userTypeId });
                
                foreach (System.Data.DataRow permRow in perms.Rows)
                {
                    var key = permRow["PermissionKey"]?.ToString() ?? string.Empty;
                    var allowed = Convert.ToBoolean(permRow["IsAllowed"]);
                    permSet.Permissions[key] = allowed;
                }
                
                result[userTypeId] = permSet;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load permissions");
        }
        
        _cachedPermissionSets = result;
        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Get permissions for a specific user type
    /// </summary>
    public async Task<UserPermissionSet> GetUserTypePermissionsAsync(int userTypeId)
    {
        if (_cachedPermissionSets?.TryGetValue(userTypeId, out var cached) == true)
            return cached;
        
        var all = await LoadAllPermissionsAsync();
        return all.TryGetValue(userTypeId, out var perms) ? perms : new UserPermissionSet { UserTypeId = userTypeId };
    }

    /// <summary>
    /// Check if a user type has a specific permission
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userTypeId, string module, string action)
    {
        var perms = await GetUserTypePermissionsAsync(userTypeId);
        return perms.HasPermission(module, action);
    }

    /// <summary>
    /// Save a single permission for a user type
    /// </summary>
    public async Task SavePermissionAsync(int userTypeId, string permissionKey, bool isAllowed)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            var existing = await db.QueryAsync(
                @"SELECT RolePermissionId FROM RolePermissions WHERE UserTypeId = @UserTypeId AND PermissionKey = @PermissionKey",
                new() { ["UserTypeId"] = userTypeId, ["PermissionKey"] = permissionKey });
            
            if (existing.Rows.Count > 0)
            {
                await db.ExecuteNonQueryAsync(
                    @"UPDATE RolePermissions SET IsAllowed = @IsAllowed, UpdatedAt = GETDATE() 
                      WHERE UserTypeId = @UserTypeId AND PermissionKey = @PermissionKey",
                    new() { ["IsAllowed"] = isAllowed, ["UserTypeId"] = userTypeId, ["PermissionKey"] = permissionKey });
            }
            else
            {
                await db.ExecuteNonQueryAsync(
                    @"INSERT INTO RolePermissions (UserTypeId, PermissionKey, IsAllowed, CreatedAt) 
                      VALUES (@UserTypeId, @PermissionKey, @IsAllowed, GETDATE())",
                    new() { ["UserTypeId"] = userTypeId, ["PermissionKey"] = permissionKey, ["IsAllowed"] = isAllowed });
            }
            
            Interlocked.Exchange(ref _cachedPermissionSets, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save permission {PermissionKey} for UserTypeId {UserTypeId}", permissionKey, userTypeId);
            throw;
        }
    }

    /// <summary>
    /// Save all permissions for a user type (bulk update)
    /// </summary>
    public async Task SaveAllPermissionsAsync(int userTypeId, Dictionary<string, bool> permissions)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            using var conn = await db.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();
            
            try
            {
                using var deleteCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "DELETE FROM RolePermissions WHERE UserTypeId = @UserTypeId", conn, transaction);
                deleteCmd.Parameters.AddWithValue("@UserTypeId", userTypeId);
                await deleteCmd.ExecuteNonQueryAsync();
                
                foreach (var (key, isAllowed) in permissions)
                {
                    using var insertCmd = new Microsoft.Data.SqlClient.SqlCommand(
                        @"INSERT INTO RolePermissions (UserTypeId, PermissionKey, IsAllowed, CreatedAt) 
                          VALUES (@UserTypeId, @PermissionKey, @IsAllowed, GETDATE())", conn, transaction);
                    insertCmd.Parameters.AddWithValue("@UserTypeId", userTypeId);
                    insertCmd.Parameters.AddWithValue("@PermissionKey", key);
                    insertCmd.Parameters.AddWithValue("@IsAllowed", isAllowed);
                    await insertCmd.ExecuteNonQueryAsync();
                }
                
                transaction.Commit();
                
                Interlocked.Exchange(ref _cachedPermissionSets, null);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save all permissions for UserTypeId {UserTypeId}", userTypeId);
            throw;
        }
    }

    /// <summary>
    /// Apply default permissions for standard role types
    /// </summary>
    public async Task ApplyDefaultPermissionsAsync()
    {
        // Director (UserTypeId = 1) - Full access except managing administrators
        await ApplyDirectorDefaultsAsync();
        
        // Administrator (UserTypeId = 2) - Full access to everything
        await ApplyAdministratorDefaultsAsync();
        
        // Accounts (UserTypeId = 3) - Access to accounting, invoices, POs, reports
        await ApplyAccountsDefaultsAsync();
        
        // Sales (UserTypeId = 4) - Access to quotes, contacts, companies, basic reports
        await ApplySalesDefaultsAsync();
    }

    private async Task ApplyDirectorDefaultsAsync()
    {
        var userTypeId = 1;
        var permissions = new Dictionary<string, bool>();
        
        // Grant all permissions
        foreach (var key in _allPermissions.Keys)
        {
            // Directors cannot manage Administrator users
            permissions[key] = key != "admin_users.manage_admins";
        }
        
        await SaveAllPermissionsAsync(userTypeId, permissions);
    }

    private async Task ApplyAdministratorDefaultsAsync()
    {
        var userTypeId = 2;
        var permissions = new Dictionary<string, bool>();
        
        // Grant ALL permissions
        foreach (var key in _allPermissions.Keys)
        {
            permissions[key] = true;
        }
        
        await SaveAllPermissionsAsync(userTypeId, permissions);
    }

    private async Task ApplyAccountsDefaultsAsync()
    {
        var userTypeId = 3;
        var permissions = new Dictionary<string, bool>();
        
        // Initialize all as false
        foreach (var key in _allPermissions.Keys)
        {
            permissions[key] = false;
        }
        
        // Accounts get access to financial modules
        var allowedModules = new[] { "dashboard", "invoices", "purchase_orders", "despatch", "reconciliation", 
            "accounting", "reports", "contacts", "companies", "files", "settings", "profile", 
            "activity", "favourites", "calendar", "expenses", "noticeboard" };
        
        foreach (var key in _allPermissions.Keys)
        {
            var module = key.Split('.')[0];
            if (allowedModules.Contains(module))
            {
                permissions[key] = true;
            }
        }
        
        // But accounts cannot delete invoices or export to MYOB (restricted)
        permissions["invoices.delete"] = false;
        
        await SaveAllPermissionsAsync(userTypeId, permissions);
    }

    private async Task ApplySalesDefaultsAsync()
    {
        var userTypeId = 4;
        var permissions = new Dictionary<string, bool>();
        
        // Initialize all as false
        foreach (var key in _allPermissions.Keys)
        {
            permissions[key] = false;
        }
        
        // Sales get access to CRM and quotes
        var allowedModules = new[] { "dashboard", "quotes", "contacts", "companies", "files", "settings", 
            "profile", "activity", "favourites", "calendar", "noticeboard", "ask_ai" };
        
        foreach (var key in _allPermissions.Keys)
        {
            var module = key.Split('.')[0];
            if (allowedModules.Contains(module))
            {
                permissions[key] = true;
            }
        }
        
        await SaveAllPermissionsAsync(userTypeId, permissions);
    }

    /// <summary>
    /// Check if a user type can manage another user type (for Director restriction on Administrators)
    /// </summary>
    public bool CanManageUserType(int managerUserTypeId, int targetUserTypeId)
    {
        // Directors cannot manage Administrator users
        if (managerUserTypeId == 1 && targetUserTypeId == 2)
            return false;
        
        return true;
    }

    /// <summary>
    /// Initialize the RolePermissions table if it doesn't exist
    /// </summary>
    public async Task InitializeTableAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            await db.ExecuteNonQueryAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RolePermissions' AND xtype='U')
                CREATE TABLE RolePermissions (
                    RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
                    UserTypeId INT NOT NULL,
                    PermissionKey NVARCHAR(100) NOT NULL,
                    IsAllowed BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME DEFAULT GETDATE(),
                    UpdatedAt DATETIME NULL,
                    CONSTRAINT FK_RolePermissions_UserTypes FOREIGN KEY (UserTypeId) REFERENCES UserTypes(UserTypeId)
                );
                
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RolePermissions_UserType_Permission')
                CREATE INDEX IX_RolePermissions_UserType_Permission ON RolePermissions(UserTypeId, PermissionKey);
            ");
            
            var count = await db.ScalarAsync<int>("SELECT COUNT(*) FROM RolePermissions");
            if (count == 0)
            {
                await ApplyDefaultPermissionsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RolePermissions table");
            throw;
        }
    }
}
