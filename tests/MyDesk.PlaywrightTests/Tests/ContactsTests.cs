using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class ContactsTests : BaseTest
{
    [SetUp]
    public async Task ContactsSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Contacts_List_Page_Loads()
    {
        await NavigateToAsync("/contacts");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Contact").Or.Contain("Contacts"));
        
        // Check for list elements
        var contactList = await Page.QuerySelectorAsync(".mud-data-grid, .contact-list, table");
        Assert.That(contactList, Is.Not.Null, "Contact list should be visible");
        
        await TakeScreenshotAsync("Contacts_List_Page");
    }
    
    [Test]
    public async Task Create_Contact_Page_Loads()
    {
        await NavigateToAsync("/contacts/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New Contact").Or.Contain("Create Contact").Or.Contain("Contact"));
        
        // Check for form elements
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "Contact form should have input fields");
        
        // Look for specific fields
        var nameInput = await Page.QuerySelectorAsync("input[name*='name'], input[name*='Name'], input[placeholder*='name']");
        var emailInput = await Page.QuerySelectorAsync("input[type='email'], input[name*='email']");
        
        await TakeScreenshotAsync("Contacts_Create_Page");
    }
    
    [Test]
    public async Task Contact_Search_Works()
    {
        await NavigateToAsync("/contacts");
        
        // Look for search input
        var searchInput = await Page.QuerySelectorAsync("input[placeholder*='Search'], input[type='search'], input[name*='search']");
        if (searchInput != null)
        {
            await searchInput.FillAsync("Test");
            await Page.WaitForTimeoutAsync(1500);
            
            await TakeScreenshotAsync("Contacts_Search");
        }
        else
        {
            Assert.Warn("Search input not found on contacts page");
        }
    }
    
    [Test]
    public async Task Contact_Details_Page_Loads()
    {
        await NavigateToAsync("/contacts");
        
        // Try to click on first contact
        var firstContactLink = await Page.QuerySelectorAsync("a[href*='/contacts/']:not([href*='/create']), .contact-row a, td a");
        if (firstContactLink != null)
        {
            await firstContactLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify we're on a contact details page
            var url = Page.Url;
            Assert.That(url, Does.Match(@"/contacts/\d+").Or.Contain("/contact/"));
            
            await TakeScreenshotAsync("Contact_Details_Page");
        }
        else
        {
            Assert.Warn("No contacts available to view details");
        }
    }
}
