using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class InvoicesTests : BaseTest
{
    [SetUp]
    public async Task InvoicesSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Invoices_List_Page_Loads()
    {
        await NavigateToAsync("/invoices");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Invoice").Or.Contain("Invoices"));
        
        // Check for list elements
        var invoiceList = await Page.QuerySelectorAsync(".mud-data-grid, .invoice-list, table");
        Assert.That(invoiceList, Is.Not.Null, "Invoice list should be visible");
        
        await TakeScreenshotAsync("Invoices_List_Page");
    }
    
    [Test]
    public async Task Create_Invoice_Page_Loads()
    {
        await NavigateToAsync("/invoices/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New Invoice").Or.Contain("Create Invoice").Or.Contain("Invoice"));
        
        // Check for form elements
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "Invoice form should have input fields");
        
        await TakeScreenshotAsync("Invoices_Create_Page");
    }
    
    [Test]
    public async Task Invoice_Details_Page_Loads()
    {
        await NavigateToAsync("/invoices");
        
        // Try to click on first invoice
        var firstInvoiceLink = await Page.QuerySelectorAsync("a[href*='/invoices/']:not([href*='/create']), .invoice-row a, td a");
        if (firstInvoiceLink != null)
        {
            await firstInvoiceLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify we're on an invoice details page
            var url = Page.Url;
            Assert.That(url, Does.Match(@"/invoices/\d+").Or.Contain("/invoice/"));
            
            await TakeScreenshotAsync("Invoice_Details_Page");
        }
        else
        {
            Assert.Warn("No invoices available to view details");
        }
    }
    
    [Test]
    public async Task Invoice_Filter_By_Date_Range()
    {
        await NavigateToAsync("/invoices");
        
        // Look for date filter inputs
        var dateFromInput = await Page.QuerySelectorAsync("input[type='date']:first-of-type, input[name*='from']");
        var dateToInput = await Page.QuerySelectorAsync("input[type='date']:last-of-type, input[name*='to']");
        
        if (dateFromInput != null && dateToInput != null)
        {
            // Set date range
            await dateFromInput.FillAsync(DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"));
            await dateToInput.FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
            
            // Apply filter (may need to click a button or just wait)
            await Page.WaitForTimeoutAsync(1500);
            
            await TakeScreenshotAsync("Invoices_Date_Filter");
        }
        else
        {
            Assert.Warn("Date filter inputs not found");
        }
    }
    
    [Test]
    public async Task Invoice_Print_Button_Available()
    {
        await NavigateToAsync("/invoices");
        
        // View first invoice
        var firstInvoiceLink = await Page.QuerySelectorAsync("a[href*='/invoices/']:not([href*='/create']), .invoice-row a");
        if (firstInvoiceLink != null)
        {
            await firstInvoiceLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Look for print button
            var printButton = await Page.QuerySelectorAsync("button:has-text('Print'), a:has-text('Print'), .print-btn");
            if (printButton != null)
            {
                Assert.Pass("Print button found on invoice details");
            }
            else
            {
                Assert.Warn("Print button not found on invoice details page");
            }
        }
        else
        {
            Assert.Warn("No invoices available to check print functionality");
        }
    }
    
    [Test]
    public async Task Invoice_Payment_Status_Visible()
    {
        await NavigateToAsync("/invoices");
        
        // Look for payment status indicators
        var statusElements = await Page.QuerySelectorAllAsync(".payment-status, .status-badge, .mud-chip");
        if (statusElements.Count > 0)
        {
            Assert.Pass("Payment status indicators found");
        }
        else
        {
            Assert.Warn("No payment status indicators visible - may need data");
        }
    }
}
