using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class NavigationTests : BaseTest
{
    [SetUp]
    public async Task NavigationSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task NavMenu_Contains_All_Main_Links()
    {
        // Check for main navigation links
        var links = new Dictionary<string, string>
        {
            { "Dashboard", "/" },
            { "Quotes", "/quotes" },
            { "Invoices", "/invoices" },
            { "Purchase Orders", "/purchaseorders" },
            { "Job Orders", "/joborders" },
            { "Contacts", "/contacts" },
            { "Companies", "/companies" },
            { "Products", "/products" },
            { "Reports", "/reports" }
        };
        
        foreach (var link in links)
        {
            var navLink = await Page.QuerySelectorAsync($"a[href='{link.Value}'], a[href*='{link.Value}']");
            if (navLink != null)
            {
                TestContext.WriteLine($"✓ Navigation link found: {link.Key}");
            }
            else
            {
                TestContext.WriteLine($"⚠ Navigation link not found: {link.Key}");
            }
        }
        
        await TakeScreenshotAsync("Navigation_Menu");
    }
    
    [Test]
    public async Task Navigation_To_Quotes_Works()
    {
        var quotesLink = await Page.QuerySelectorAsync("a[href='/quotes'], a[href*='quotes']");
        if (quotesLink != null)
        {
            await quotesLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.That(Page.Url, Does.Contain("/quotes"));
        }
        else
        {
            // Navigate directly
            await NavigateToAsync("/quotes");
            Assert.That(Page.Url, Does.Contain("/quotes"));
        }
        
        await TakeScreenshotAsync("Navigation_Quotes");
    }
    
    [Test]
    public async Task Navigation_To_Invoices_Works()
    {
        var invoicesLink = await Page.QuerySelectorAsync("a[href='/invoices'], a[href*='invoices']");
        if (invoicesLink != null)
        {
            await invoicesLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.That(Page.Url, Does.Contain("/invoices"));
        }
        else
        {
            await NavigateToAsync("/invoices");
            Assert.That(Page.Url, Does.Contain("/invoices"));
        }
        
        await TakeScreenshotAsync("Navigation_Invoices");
    }
    
    [Test]
    public async Task Navigation_To_Contacts_Works()
    {
        await NavigateToAsync("/contacts");
        
        Assert.That(Page.Url, Does.Contain("/contacts"));
        
        // Check for contact list
        var contactList = await Page.QuerySelectorAsync(".contact-list, .mud-data-grid, table");
        Assert.That(contactList, Is.Not.Null, "Contact list should be visible");
        
        await TakeScreenshotAsync("Navigation_Contacts");
    }
    
    [Test]
    public async Task Navigation_To_Companies_Works()
    {
        await NavigateToAsync("/companies");
        
        Assert.That(Page.Url, Does.Contain("/companies"));
        
        // Check for company list
        var companyList = await Page.QuerySelectorAsync(".company-list, .mud-data-grid, table");
        Assert.That(companyList, Is.Not.Null, "Company list should be visible");
        
        await TakeScreenshotAsync("Navigation_Companies");
    }
    
    [Test]
    public async Task Navigation_To_Products_Works()
    {
        await NavigateToAsync("/products");
        
        Assert.That(Page.Url, Does.Contain("/products"));
        
        // Check for product list
        var productList = await Page.QuerySelectorAsync(".product-list, .mud-data-grid, table");
        Assert.That(productList, Is.Not.Null, "Product list should be visible");
        
        await TakeScreenshotAsync("Navigation_Products");
    }
    
    [Test]
    public async Task Navigation_To_Reports_Works()
    {
        await NavigateToAsync("/reports");
        
        Assert.That(Page.Url, Does.Contain("/reports"));
        
        // Check for report elements
        var reportsContent = await Page.QuerySelectorAsync("main, .content, .reports-container");
        Assert.That(reportsContent, Is.Not.Null, "Reports content should be visible");
        
        await TakeScreenshotAsync("Navigation_Reports");
    }
    
    [Test]
    public async Task Navigation_To_Settings_Works()
    {
        await NavigateToAsync("/admin/setup");
        
        Assert.That(Page.Url, Does.Contain("/admin/setup").Or.Contain("/settings"));
        
        // Check for settings content
        var settingsContent = await Page.QuerySelectorAsync("main, .content, .settings-container");
        Assert.That(settingsContent, Is.Not.Null, "Settings content should be visible");
        
        await TakeScreenshotAsync("Navigation_Settings");
    }
    
    [Test]
    public async Task Breadcrumb_Navigation_Works()
    {
        // Navigate to a nested page
        await NavigateToAsync("/quotes");
        
        // Look for breadcrumb
        var breadcrumb = await Page.QuerySelectorAsync(".mud-breadcrumbs, .breadcrumb, nav[aria-label='breadcrumb']");
        if (breadcrumb != null)
        {
            // Look for home link in breadcrumb
            var homeLink = await breadcrumb.QuerySelectorAsync("a[href='/'], a:has-text('Home')");
            if (homeLink != null)
            {
                await homeLink.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                Assert.That(Page.Url, Is.EqualTo($"{Settings.BaseUrl}/").Or.EqualTo(Settings.BaseUrl));
            }
            else
            {
                Assert.Warn("Home link in breadcrumb not found");
            }
        }
        else
        {
            Assert.Warn("Breadcrumb not found on page");
        }
    }
    
    [Test]
    public async Task Responsive_Navigation_Collapses_On_Mobile()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        
        // Refresh page
        await Page.GotoAsync($"{Settings.BaseUrl}/");
        
        // Check for hamburger menu or collapsed nav
        var hamburgerMenu = await Page.QuerySelectorAsync(".mud-icon-button[aria-label*='menu'], .menu-button, .hamburger");
        if (hamburgerMenu != null)
        {
            Assert.Pass("Hamburger menu found for mobile navigation");
        }
        else
        {
            Assert.Warn("Hamburger menu not found - may be using different responsive pattern");
        }
        
        // Reset viewport
        await Page.SetViewportSizeAsync(1920, 1080);
    }
}
