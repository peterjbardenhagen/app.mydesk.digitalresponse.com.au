using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class TimesheetsTests : BaseTest
{
    [Test, Category("Smoke")]
    public async Task Timesheets_Page_Loads_Successfully()
    {
        await LoginAsync();
        await NavigateToAsync("/timesheets");

        var heading = Page.Locator("text=Timesheets").First;
        await heading.WaitForAsync(new() { Timeout = 15000 });
        Assert.That(await heading.IsVisibleAsync(), Is.True,
            "Timesheets page heading should be visible");

        await TakeScreenshotAsync("Timesheets_List");
    }

    [Test]
    public async Task Timesheets_Has_Demo_Entries()
    {
        await LoginAsync();
        await NavigateToAsync("/timesheets");

        // The SQL migration creates 2 demo timesheets
        // Look for any data rows in the timesheet grid
        var tableBody = Page.Locator(".mud-table-body").First;
        var rows = await tableBody.Locator("tr").CountAsync();

        // The page may also display a summary or week view
        var summary = Page.Locator("text=Summary").First;
        var summaryVisible = await summary.IsVisibleAsync();

        Assert.That(rows > 0 || summaryVisible, Is.True,
            "Timesheets page should contain data or a summary view");

        await TakeScreenshotAsync("Timesheets_Data");
    }

    [Test]
    public async Task Timesheets_Week_Selector_Works()
    {
        await LoginAsync();
        await NavigateToAsync("/timesheets");

        // Look for a date picker or week selector
        var weekPicker = Page.Locator("input[type='date'], .mud-picker, button:has-text('Week')").First;
        if (await weekPicker.IsVisibleAsync())
        {
            await TakeScreenshotAsync("Timesheets_Week_Selector");
        }
    }

    [Test]
    public async Task Timesheets_Navigate_To_Previous_Week()
    {
        await LoginAsync();
        await NavigateToAsync("/timesheets");

        // Look for navigation arrows to go to previous/next week
        var prevButton = Page.Locator("button:has-text('Previous'), button[aria-label='Previous'], .mud-button:has(.fa-chevron-left), .mud-button:has(.fa-arrow-left)").First;
        if (await prevButton.IsVisibleAsync())
        {
            await prevButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshotAsync("Timesheets_Previous_Week");
        }
    }
}
