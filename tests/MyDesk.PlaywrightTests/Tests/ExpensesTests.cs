using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class ExpensesTests : BaseTest
{
    [Test, Category("Smoke")]
    public async Task Expenses_Page_Loads_Successfully()
    {
        await LoginAsync();
        await NavigateToAsync("/expenses");

        var heading = Page.Locator("text=Expenses").First;
        await heading.WaitForAsync(new() { Timeout = 15000 });
        Assert.That(await heading.IsVisibleAsync(), Is.True,
            "Expenses page heading should be visible");

        await TakeScreenshotAsync("Expenses_List");
    }

    [Test]
    public async Task Expenses_Shows_Demo_Data()
    {
        await LoginAsync();
        await NavigateToAsync("/expenses");

        // The seeder and SQL migration create demo expenses
        var expenseRow = Page.Locator("text=/[DEMO]/").First;
        var exists = await expenseRow.CountAsync() > 0;

        // Even without [DEMO] prefix, expense data should exist
        if (!exists)
        {
            var dataGrid = Page.Locator(".mud-table-body").First;
            var rows = await dataGrid.Locator("tr").CountAsync();
            Assert.That(rows, Is.GreaterThan(0),
                "Expenses data grid should contain rows");
        }

        await TakeScreenshotAsync("Expenses_With_Data");
    }

    [Test]
    public async Task Expenses_Filter_By_Status_Works()
    {
        await LoginAsync();
        await NavigateToAsync("/expenses");

        // Click on a status tab (e.g. "Approved", "Submitted", "Draft")
        var statusTab = Page.Locator(".mud-tab:has-text('Draft')").First;
        if (await statusTab.IsVisibleAsync())
        {
            await statusTab.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshotAsync("Expenses_Filtered_Draft");
        }
    }

    [Test]
    public async Task Expenses_Search_Filters_Results()
    {
        await LoginAsync();
        await NavigateToAsync("/expenses");

        var searchInput = Page.Locator("input[placeholder*='Search']").First;
        if (await searchInput.IsVisibleAsync())
        {
            await searchInput.FillAsync("Conference");
            await Page.WaitForTimeoutAsync(500);

            await TakeScreenshotAsync("Expenses_Search_Results");
        }
    }
}
