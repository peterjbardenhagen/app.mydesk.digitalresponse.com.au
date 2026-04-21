using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class CompaniesTests : BaseTest
{
    [SetUp]
    public async Task CompaniesSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Companies_List_Page_Loads()
    {
        await NavigateToAsync("/companies");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Company").Or.Contain("Companies"));
        
        // Check for list elements
        var companyList = await Page.QuerySelectorAsync(".mud-data-grid, .company-list, table");
        Assert.That(companyList, Is.Not.Null, "Company list should be visible");
        
        await TakeScreenshotAsync("Companies_List_Page");
    }
    
    [Test]
    public async Task Create_Company_Page_Loads()
    {
        await NavigateToAsync("/companies/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New Company").Or.Contain("Create Company").Or.Contain("Company"));
        
        // Check for form elements
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "Company form should have input fields");
        
        await TakeScreenshotAsync("Companies_Create_Page");
    }
    
    [Test]
    public async Task Company_Details_Page_Loads()
    {
        await NavigateToAsync("/companies");
        
        // Try to click on first company
        var firstCompanyLink = await Page.QuerySelectorAsync("a[href*='/companies/']:not([href*='/create']), .company-row a, td a");
        if (firstCompanyLink != null)
        {
            await firstCompanyLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify we're on a company details page
            var url = Page.Url;
            Assert.That(url, Does.Match(@"/companies/\d+").Or.Contain("/company/"));
            
            await TakeScreenshotAsync("Company_Details_Page");
        }
        else
        {
            Assert.Warn("No companies available to view details");
        }
    }
}
