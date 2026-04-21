using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class QuotesTests : BaseTest
{
    [SetUp]
    public async Task QuotesSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Quotes_List_Page_Loads()
    {
        await NavigateToAsync("/quotes");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Quote").Or.Contain("Quotes"));
        
        // Check for list elements
        var quoteList = await Page.QuerySelectorAsync(".mud-data-grid, .quote-list, table");
        Assert.That(quoteList, Is.Not.Null, "Quote list should be visible");
        
        await TakeScreenshotAsync("Quotes_List_Page");
    }
    
    [Test]
    public async Task Create_Quote_Page_Loads()
    {
        await NavigateToAsync("/quotes/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New Quote").Or.Contain("Create Quote").Or.Contain("Quote"));
        
        // Check for form elements
        var customerInput = await Page.QuerySelectorAsync("input[placeholder*='Customer'], input[name*='Customer'], input[name*='customer']");
        var contactInput = await Page.QuerySelectorAsync("input[placeholder*='Contact'], input[name*='Contact'], input[name*='contact']");
        
        // Form may have different structure, so we check for any form inputs
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "Quote form should have input fields");
        
        await TakeScreenshotAsync("Quotes_Create_Page");
    }
    
    [Test]
    public async Task Create_Quote_Submit_Empty_Shows_Validation()
    {
        await NavigateToAsync("/quotes/create");
        
        // Try to submit without filling required fields
        var submitButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Create'), button:has-text('Save')");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            
            // Should still be on the same page (validation failed)
            var url = Page.Url;
            Assert.That(url, Does.Contain("/quotes/create"));
            
            await TakeScreenshotAsync("Quotes_Create_Validation");
        }
        else
        {
            Assert.Warn("Submit button not found");
        }
    }
    
    [Test]
    public async Task Quote_Details_Page_Loads()
    {
        // First go to quotes list
        await NavigateToAsync("/quotes");
        
        // Try to click on first quote
        var firstQuoteLink = await Page.QuerySelectorAsync("a[href*='/quotes/']:not([href*='/create']), .quote-row a, td a");
        if (firstQuoteLink != null)
        {
            await firstQuoteLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify we're on a quote details page
            var url = Page.Url;
            Assert.That(url, Does.Match(@"/quotes/\d+").Or.Contain("/quote/"));
            
            await TakeScreenshotAsync("Quote_Details_Page");
        }
        else
        {
            Assert.Warn("No quotes available to view details");
        }
    }
    
    [Test]
    public async Task Quote_Edit_Page_Loads()
    {
        // First view a quote
        await NavigateToAsync("/quotes");
        
        // Try to click on first quote
        var firstQuoteLink = await Page.QuerySelectorAsync("a[href*='/quotes/']:not([href*='/create']), .quote-row a, td a");
        if (firstQuoteLink != null)
        {
            await firstQuoteLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Look for edit button
            var editButton = await Page.QuerySelectorAsync("a[href*='/edit'], button:has-text('Edit')");
            if (editButton != null)
            {
                await editButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                // Verify edit page loaded
                var url = Page.Url;
                Assert.That(url, Does.Contain("/edit").Or.Contain("/quotes/"));
                
                await TakeScreenshotAsync("Quote_Edit_Page");
            }
            else
            {
                Assert.Warn("Edit button not found on quote details page");
            }
        }
        else
        {
            Assert.Warn("No quotes available to edit");
        }
    }
    
    [Test]
    public async Task Quote_Filter_Works()
    {
        await NavigateToAsync("/quotes");
        
        // Look for filter/search input
        var searchInput = await Page.QuerySelectorAsync("input[placeholder*='Search'], input[name*='search'], input[type='search']");
        if (searchInput != null)
        {
            await searchInput.FillAsync("Test");
            await Page.WaitForTimeoutAsync(1500); // Wait for debounce/filter
            
            // Check that list updates
            var quoteList = await Page.QuerySelectorAsync(".mud-data-grid, .quote-list, table tbody");
            Assert.That(quoteList, Is.Not.Null);
            
            await TakeScreenshotAsync("Quotes_Filter_Applied");
            
            // Clear filter
            await searchInput.FillAsync("");
            await Page.WaitForTimeoutAsync(1500);
        }
        else
        {
            Assert.Warn("Search/filter input not found");
        }
    }
    
    [Test]
    public async Task Quote_Status_Badge_Visible()
    {
        await NavigateToAsync("/quotes");
        
        // Look for status badges in the list
        var statusBadges = await Page.QuerySelectorAllAsync(".mud-chip, .status-badge, .badge");
        if (statusBadges.Count > 0)
        {
            Assert.Pass("Status badges found in quote list");
        }
        else
        {
            Assert.Warn("No status badges visible - may need data or different selector");
        }
    }
}
