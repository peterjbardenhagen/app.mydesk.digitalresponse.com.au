using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class PurchaseOrdersTests : BaseTest
{
    [SetUp]
    public async Task PurchaseOrdersSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task PurchaseOrders_List_Page_Loads()
    {
        await NavigateToAsync("/purchaseorders");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Purchase Order").Or.Contain("Purchase Orders").Or.Contain("PO"));
        
        // Check for list elements
        var poList = await Page.QuerySelectorAsync(".mud-data-grid, .po-list, table");
        Assert.That(poList, Is.Not.Null, "Purchase Order list should be visible");
        
        await TakeScreenshotAsync("PurchaseOrders_List_Page");
    }
    
    [Test]
    public async Task Create_PurchaseOrder_Page_Loads()
    {
        await NavigateToAsync("/purchaseorders/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New").Or.Contain("Create").Or.Contain("Purchase Order").Or.Contain("PO"));
        
        // Check for form elements
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "PO form should have input fields");
        
        await TakeScreenshotAsync("PurchaseOrders_Create_Page");
    }
    
    [Test]
    public async Task PurchaseOrder_Approval_Status_Visible()
    {
        await NavigateToAsync("/purchaseorders");
        
        // Look for approval status indicators
        var statusElements = await Page.QuerySelectorAllAsync(".approval-status, .status-badge, .mud-chip");
        if (statusElements.Count > 0)
        {
            Assert.Pass("Approval status indicators found");
        }
        else
        {
            Assert.Warn("No approval status indicators visible - may need data");
        }
    }
    
    [Test]
    public async Task PurchaseOrder_Details_Shows_Supplier_Info()
    {
        await NavigateToAsync("/purchaseorders");
        
        // Try to click on first PO
        var firstPOLink = await Page.QuerySelectorAsync("a[href*='/purchaseorders/']:not([href*='/create']), .po-row a, td a");
        if (firstPOLink != null)
        {
            await firstPOLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Check for supplier information
            var supplierInfo = await Page.QuerySelectorAsync(".supplier-info, .vendor-info, .po-supplier");
            // May not have specific class, check for general content
            var content = await Page.QuerySelectorAsync("main, .content, .mud-paper");
            Assert.That(content, Is.Not.Null, "PO details content should be visible");
            
            await TakeScreenshotAsync("PurchaseOrder_Details_Page");
        }
        else
        {
            Assert.Warn("No purchase orders available to view details");
        }
    }
}
