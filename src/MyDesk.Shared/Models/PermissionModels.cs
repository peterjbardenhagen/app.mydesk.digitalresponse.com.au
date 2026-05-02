namespace MyDesk.Shared.Models;

/// <summary>
/// Represents a single permission (e.g., "quotes.add", "quotes.edit", "users.view")
/// </summary>
public class PermissionDefinition
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullKey => $"{Module}.{Action}";
}

/// <summary>
/// Granular permission stored in database per role
/// </summary>
public class RolePermission
{
    public int RolePermissionId { get; set; }
    public int UserTypeId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsAllowed { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Complete permission set for a user type (cached)
/// </summary>
public class UserPermissionSet
{
    public int UserTypeId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Dictionary<string, bool> Permissions { get; set; } = new();
    
    public bool HasPermission(string module, string action) => 
        Permissions.TryGetValue($"{module}.{action}", out var allowed) && allowed;
    
    public bool HasAnyPermission(string module) => 
        Permissions.Any(kvp => kvp.Key.StartsWith($"{module}.") && kvp.Value);
}

/// <summary>
/// Module definition for permission matrix
/// </summary>
public class PermissionModule
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<PermissionDefinition> Actions { get; set; } = new();
}
