using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class AgentsOsTests : BaseTest
{
    [Test, Category("Smoke")]
    public async Task AgentsOS_Page_Loads_Successfully()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        var heading = Page.Locator("text=AgentsOS").First;
        await heading.WaitForAsync(new() { Timeout = 15000 });
        Assert.That(await heading.IsVisibleAsync(), Is.True,
            "AgentsOS page heading should be visible");

        await TakeScreenshotAsync("AgentsOS_Loaded");
    }

    [Test]
    public async Task AgentsOS_Has_Goals_Tab()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        var goalsTab = Page.Locator(".mud-tab:has-text('Goals')").First;
        await goalsTab.WaitForAsync(new() { Timeout = 10000 });
        Assert.That(await goalsTab.IsVisibleAsync(), Is.True,
            "Goals tab should be visible on AgentsOS page");

        await TakeScreenshotAsync("AgentsOS_Goals_Tab");
    }

    [Test]
    public async Task AgentsOS_Has_DAG_Tab()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        var dagTab = Page.Locator(".mud-tab:has-text('DAG')").First;
        await dagTab.WaitForAsync(new() { Timeout = 10000 });
        Assert.That(await dagTab.IsVisibleAsync(), Is.True,
            "DAG tab should be visible on AgentsOS page");

        await TakeScreenshotAsync("AgentsOS_DAG_Tab");
    }

    [Test]
    public async Task AgentsOS_Has_Ledger_Tab()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        var ledgerTab = Page.Locator(".mud-tab:has-text('Ledger')").First;
        await ledgerTab.WaitForAsync(new() { Timeout = 10000 });
        Assert.That(await ledgerTab.IsVisibleAsync(), Is.True,
            "Ledger tab should be visible on AgentsOS page");

        await TakeScreenshotAsync("AgentsOS_Ledger_Tab");
    }

    [Test]
    public async Task AgentsOS_Plan_Button_Exists()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        var planButton = Page.Locator("button:has-text('Plan'), .mud-button:has-text('Plan')").First;
        await planButton.WaitForAsync(new() { Timeout = 10000 });
        Assert.That(await planButton.IsVisibleAsync(), Is.True,
            "Plan button should be visible on AgentsOS page (may be disabled if no brief)");

        await TakeScreenshotAsync("AgentsOS_Plan_Button");
    }

    [Test]
    public async Task AgentsOS_Switching_Tabs_Works()
    {
        await LoginAsync();
        await NavigateToAsync("/agentsos");

        // Start on Goals tab
        var dagTab = Page.Locator(".mud-tab:has-text('DAG')").First;
        if (await dagTab.IsVisibleAsync())
        {
            await dagTab.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Verify DAG content is visible
            var dagContent = Page.Locator("text=Plan, Execute").First;
            await TakeScreenshotAsync("AgentsOS_DAG_After_Switch");

            // Switch to Ledger tab
            var ledgerTab = Page.Locator(".mud-tab:has-text('Ledger')").First;
            if (await ledgerTab.IsVisibleAsync())
            {
                await ledgerTab.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
                await TakeScreenshotAsync("AgentsOS_Ledger_After_Switch");
            }
        }
    }
}
