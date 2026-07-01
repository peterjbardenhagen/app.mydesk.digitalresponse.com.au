using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services.Integrations;

/// <summary>
/// Minimal abstraction over PlatformSettingsService used by the accounting
/// sync services, allowing them to live in MyDesk.Shared without a reference
/// to MyDesk.Web.
/// </summary>
public interface IAccountingSettingsService
{
    PlatformSettings Current { get; }
    Task SaveAsync(PlatformSettings? settings = null);
}
