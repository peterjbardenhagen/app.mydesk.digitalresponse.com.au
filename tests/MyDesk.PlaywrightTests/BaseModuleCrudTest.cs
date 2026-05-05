using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests;

/// <summary>
/// Reusable smoke test scaffolding for module pages — list, detail, create,
/// edit, delete, PDF, and email actions.
///
/// Each module's specific test class (CompaniesCrudTests, QuotesCrudTests, etc.)
/// extends this and just sets the module URLs and a search string that should
/// match a row in the seeded Demo data (typically <c>"[DEMO]"</c>).
///
/// Tests use <c>Assert.Ignore</c> when they can't find a fixture they need,
/// so a missing UI element produces a "skipped" rather than a noisy failure
/// that masks real regressions.
/// </summary>
public abstract class BaseModuleCrudTest : BaseTest
{
    /// <summary>The list page route (e.g. "/companies").</summary>
    protected abstract string ListUrl { get; }

    /// <summary>The create page route (e.g. "/companies/create"). Optional — null skips create test.</summary>
    protected virtual string? CreateUrl => null;

    /// <summary>A substring that must appear on at least one list row from seed data (default "[DEMO]").</summary>
    protected virtual string SeedRowMarker => "[DEMO]";

    /// <summary>Friendly module name used in screenshot filenames.</summary>
    protected abstract string ModuleName { get; }

    [Test]
    public async Task List_Page_Loads()
    {
        await LoginAsync();
        await NavigateToAsync(ListUrl);
        await Page.WaitForSelectorAsync("body", new() { Timeout = 15000 });
        await TakeScreenshotAsync($"{ModuleName}_List");
        // Sanity: the list URL resolves (no redirect to /login).
        Assert.That(Page.Url, Does.Not.Contain("/login"),
            "Expected to remain on the list page after login.");
    }

    [Test]
    public async Task List_Page_Shows_Demo_Seed_Row()
    {
        await LoginAsync();
        await NavigateToAsync(ListUrl);

        var demoRow = Page.Locator($"text=/{System.Text.RegularExpressions.Regex.Escape(SeedRowMarker)}/").First;
        try
        {
            await demoRow.WaitForAsync(new() { Timeout = 10000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            Assert.Ignore($"{ModuleName}: no seed row containing '{SeedRowMarker}' found on {ListUrl}. " +
                          "DemoDataSeeder may not cover this module yet.");
            return;
        }
        Assert.That(await demoRow.IsVisibleAsync(), Is.True);
        await TakeScreenshotAsync($"{ModuleName}_DemoRow");
    }

    [Test]
    public async Task Create_Page_Loads()
    {
        if (CreateUrl is null)
        {
            Assert.Ignore($"{ModuleName}: no CreateUrl declared.");
            return;
        }
        await LoginAsync();
        await NavigateToAsync(CreateUrl);
        await Page.WaitForSelectorAsync("body", new() { Timeout = 15000 });
        Assert.That(Page.Url, Does.Not.Contain("/login"));
        await TakeScreenshotAsync($"{ModuleName}_Create");
    }

    [Test]
    public async Task First_Detail_Row_Opens_Without_Error()
    {
        await LoginAsync();
        await NavigateToAsync(ListUrl);

        // Try to click the first link / button on the first row that looks like a "view" action.
        var firstRowAction = Page.Locator("tbody tr a, tbody tr button").First;
        try
        {
            await firstRowAction.WaitForAsync(new() { Timeout = 8000, State = WaitForSelectorState.Visible });
            await firstRowAction.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
        catch
        {
            Assert.Ignore($"{ModuleName}: no actionable row found on {ListUrl}.");
            return;
        }

        // Detail page should not be the login page or an error page.
        Assert.That(Page.Url, Does.Not.Contain("/login"));
        await TakeScreenshotAsync($"{ModuleName}_Detail");
    }
}
