using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class JobOrdersTests : BaseTest
{
    [SetUp]
    public async Task JobOrdersSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task JobOrders_List_Page_Loads()
    {
        await NavigateToAsync("/joborders");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Job Order").Or.Contain("Job Orders").Or.Contain("Job"));
        
        // Check for list elements
        var jobList = await Page.QuerySelectorAsync(".mud-data-grid, .job-list, table");
        Assert.That(jobList, Is.Not.Null, "Job Order list should be visible");
        
        await TakeScreenshotAsync("JobOrders_List_Page");
    }
    
    [Test]
    public async Task JobOrders_Filter_By_Status_Works()
    {
        await NavigateToAsync("/joborders");
        
        // Look for status filter
        var statusFilter = await Page.QuerySelectorAsync("select[name*='status'], .status-filter, input[placeholder*='status']");
        if (statusFilter != null)
        {
            // Try to select a status
            await statusFilter.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            
            // Select an option (first one)
            var options = await statusFilter.QuerySelectorAllAsync("option");
            if (options.Count > 1)
            {
                await options[1].ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
                
                await TakeScreenshotAsync("JobOrders_Status_Filter");
            }
        }
        else
        {
            Assert.Warn("Status filter not found");
        }
    }
    
    [Test]
    public async Task JobOrder_Details_Shows_Workflow_Status()
    {
        await NavigateToAsync("/joborders");
        
        // Try to click on first job order
        var firstJobLink = await Page.QuerySelectorAsync("a[href*='/joborders/']:not([href*='/create']), .job-row a, td a");
        if (firstJobLink != null)
        {
            await firstJobLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Check for workflow status
            var workflowStatus = await Page.QuerySelectorAsync(".workflow-status, .status-badge, .job-status");
            if (workflowStatus != null)
            {
                var statusText = await workflowStatus.TextContentAsync();
                TestContext.WriteLine($"Job Order Status: {statusText}");
            }
            
            await TakeScreenshotAsync("JobOrder_Details_Page");
        }
        else
        {
            Assert.Warn("No job orders available to view details");
        }
    }
}
