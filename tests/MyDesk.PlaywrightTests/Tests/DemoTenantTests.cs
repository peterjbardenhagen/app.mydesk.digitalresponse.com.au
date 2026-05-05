using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

/// <summary>
/// Smoke tests that confirm the Demo MyDesk tenant + DemoDataSeeder are wired up.
///
/// These tests rely on:
///   * <c>TestSettings.TenantSlug = "demo"</c> in <c>appsettings.json</c> so the
///     login helper picks the Demo tenant on the multi-tenant chooser.
///   * The DemoDataSeeder having run on app startup — it inserts companies/contacts/
///     quotes/etc. all prefixed with <c>[DEMO]</c>.
/// </summary>
[TestFixture]
public class DemoTenantTests : BaseTest
{
    [Test]
    public async Task After_Login_Should_Be_In_Demo_Tenant()
    {
        await LoginAsync();

        // The tenant name should appear somewhere in the chrome (header nav, dashboard, etc.).
        // The branding is purple/yellow on the login page; once inside, a tenant switcher
        // or page title typically shows the active tenant name.
        var html = await Page.ContentAsync();
        StringAssert.Contains("Demo", html, "Page should reflect the Demo tenant context after login.");
        await TakeScreenshotAsync("Demo_Logged_In");
    }

    [Test]
    public async Task Companies_List_Shows_Demo_Seed_Data()
    {
        await LoginAsync();
        await NavigateToAsync("/companies");

        // The seeder creates rows whose CompanyName starts with "[DEMO]".
        // Wait for at least one such cell to render in the companies list.
        var demoRow = Page.Locator("text=/\\[DEMO\\]/").First;
        await demoRow.WaitForAsync(new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        Assert.That(await demoRow.IsVisibleAsync(), Is.True,
            "Companies list should contain at least one [DEMO] row from DemoDataSeeder.");
        await TakeScreenshotAsync("Demo_Companies_List");
    }

    [Test]
    public async Task Quotes_List_Shows_Demo_Seed_Data()
    {
        await LoginAsync();
        await NavigateToAsync("/quotes");

        var demoRow = Page.Locator("text=/\\[DEMO\\]/").First;
        await demoRow.WaitForAsync(new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        Assert.That(await demoRow.IsVisibleAsync(), Is.True,
            "Quotes list should contain at least one [DEMO] row from DemoDataSeeder.");
        await TakeScreenshotAsync("Demo_Quotes_List");
    }

    [Test]
    public async Task Scheduled_Tasks_Page_Loads_With_Demo_Tasks()
    {
        await LoginAsync();
        await NavigateToAsync("/admin/scheduled-tasks");

        // Page heading should be present
        var heading = Page.Locator("text=Scheduled Tasks").First;
        await heading.WaitForAsync(new() { Timeout = 15000 });

        // The seeder creates two tasks both starting with "[DEMO]"
        var demoTask = Page.Locator("text=/\\[DEMO\\] Weekly pipeline summary/").First;
        await demoTask.WaitForAsync(new() { Timeout = 10000 });
        Assert.That(await demoTask.IsVisibleAsync(), Is.True);
        await TakeScreenshotAsync("Demo_Scheduled_Tasks");
    }
}
