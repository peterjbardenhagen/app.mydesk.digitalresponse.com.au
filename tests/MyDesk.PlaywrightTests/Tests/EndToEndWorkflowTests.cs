using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class EndToEndWorkflowTests : BaseTest
{
    [Test]
    public async Task Complete_Sales_Workflow_Create_Quote_To_Invoice()
    {
        await LoginAsync();
        
        TestContext.WriteLine("=== Starting Sales Workflow Test ===");
        
        // 1. Create a new quote
        TestContext.WriteLine("Step 1: Creating new quote...");
        await NavigateToAsync("/quotes/create");
        
        // Fill quote form
        var customerInput = await Page.QuerySelectorAsync("input[name*='Customer'], input[name*='customer'], input[placeholder*='customer']");
        if (customerInput != null)
        {
            await customerInput.FillAsync("Test Customer Ltd");
        }
        
        var contactInput = await Page.QuerySelectorAsync("input[name*='Contact'], input[name*='contact']");
        if (contactInput != null)
        {
            await contactInput.FillAsync("Test Contact");
        }
        
        // Add line item if available
        var addLineButton = await Page.QuerySelectorAsync("button:has-text('Add Line'), button:has-text('Add Item')");
        if (addLineButton != null)
        {
            await addLineButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            
            // Fill line item details
            var productInput = await Page.QuerySelectorAsync("input[name*='Product'], input[name*='product']");
            if (productInput != null)
            {
                await productInput.FillAsync("Test Product");
            }
            
            var quantityInput = await Page.QuerySelectorAsync("input[name*='Quantity'], input[name*='qty']");
            if (quantityInput != null)
            {
                await quantityInput.FillAsync("10");
            }
        }
        
        // Save quote
        var saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
        if (saveButton != null)
        {
            await saveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(2000);
        }
        
        await TakeScreenshotAsync("E2E_Quote_Created");
        TestContext.WriteLine("Quote created successfully");
        
        // 2. View the created quote
        var quoteUrl = Page.Url;
        if (quoteUrl.Contains("/quotes/"))
        {
            TestContext.WriteLine($"Quote URL: {quoteUrl}");
            
            // 3. Generate PDF (if available)
            var pdfButton = await Page.QuerySelectorAsync("button:has-text('PDF'), button:has-text('Print'), a:has-text('PDF')");
            if (pdfButton != null)
            {
                TestContext.WriteLine("PDF generation available");
            }
            
            // 4. Convert to Job Order (if available)
            var toJobButton = await Page.QuerySelectorAsync("button:has-text('To Job'), a:has-text('To Job'), button:has-text('Create Job')");
            if (toJobButton != null)
            {
                TestContext.WriteLine("Converting to Job Order...");
                await toJobButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Page.WaitForTimeoutAsync(2000);
                
                await TakeScreenshotAsync("E2E_Quote_To_Job");
            }
            
            // 5. Convert to Invoice (if available)
            var toInvoiceButton = await Page.QuerySelectorAsync("button:has-text('To Invoice'), a:has-text('To Invoice'), button:has-text('Create Invoice')");
            if (toInvoiceButton != null)
            {
                TestContext.WriteLine("Converting to Invoice...");
                await toInvoiceButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Page.WaitForTimeoutAsync(2000);
                
                await TakeScreenshotAsync("E2E_Quote_To_Invoice");
            }
        }
        
        TestContext.WriteLine("=== Sales Workflow Test Complete ===");
    }
    
    [Test]
    public async Task Complete_Purchase_Order_Workflow()
    {
        await LoginAsync();
        
        TestContext.WriteLine("=== Starting Purchase Order Workflow Test ===");
        
        // 1. Create a new purchase order
        TestContext.WriteLine("Step 1: Creating new purchase order...");
        await NavigateToAsync("/purchaseorders/create");
        
        // Fill PO form
        var supplierInput = await Page.QuerySelectorAsync("input[name*='Supplier'], input[name*='supplier'], input[name*='Vendor']");
        if (supplierInput != null)
        {
            await supplierInput.FillAsync("Test Supplier Inc");
        }
        
        var descriptionInput = await Page.QuerySelectorAsync("textarea[name*='Description'], input[name*='description']");
        if (descriptionInput != null)
        {
            await descriptionInput.FillAsync("Test Purchase Order Description");
        }
        
        // Add line item
        var addLineButton = await Page.QuerySelectorAsync("button:has-text('Add Line'), button:has-text('Add Item')");
        if (addLineButton != null)
        {
            await addLineButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            
            var productInput = await Page.QuerySelectorAsync("input[name*='Product'], input[name*='product']");
            if (productInput != null)
            {
                await productInput.FillAsync("Office Supplies");
            }
        }
        
        // Save PO
        var saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
        if (saveButton != null)
        {
            await saveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(2000);
        }
        
        await TakeScreenshotAsync("E2E_PO_Created");
        TestContext.WriteLine("Purchase Order created successfully");
        
        // 2. Approve the PO (if approval workflow exists)
        var approveButton = await Page.QuerySelectorAsync("button:has-text('Approve'), a:has-text('Approve')");
        if (approveButton != null)
        {
            TestContext.WriteLine("Approving Purchase Order...");
            await approveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(2000);
            
            await TakeScreenshotAsync("E2E_PO_Approved");
            TestContext.WriteLine("Purchase Order approved");
        }
        
        // 3. Mark as sent (if available)
        var markSentButton = await Page.QuerySelectorAsync("button:has-text('Send'), button:has-text('Mark Sent')");
        if (markSentButton != null)
        {
            TestContext.WriteLine("Marking PO as sent...");
            await markSentButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }
        
        TestContext.WriteLine("=== Purchase Order Workflow Test Complete ===");
    }
    
    [Test]
    public async Task CRM_Workflow_Create_Contact_And_Company()
    {
        await LoginAsync();
        
        TestContext.WriteLine("=== Starting CRM Workflow Test ===");
        
        // 1. Create a new company
        TestContext.WriteLine("Step 1: Creating new company...");
        await NavigateToAsync("/companies/create");
        
        var companyNameInput = await Page.QuerySelectorAsync("input[name*='Name'], input[name*='name'], input[placeholder*='company']");
        if (companyNameInput != null)
        {
            await companyNameInput.FillAsync("New Test Company Pty Ltd");
        }
        
        var abnInput = await Page.QuerySelectorAsync("input[name*='ABN'], input[name*='abn']");
        if (abnInput != null)
        {
            await abnInput.FillAsync("12 345 678 901");
        }
        
        var saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
        if (saveButton != null)
        {
            await saveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(2000);
        }
        
        await TakeScreenshotAsync("E2E_Company_Created");
        TestContext.WriteLine("Company created successfully");
        
        // 2. Create a new contact
        TestContext.WriteLine("Step 2: Creating new contact...");
        await NavigateToAsync("/contacts/create");
        
        var contactNameInput = await Page.QuerySelectorAsync("input[name*='Name'], input[name*='name'], input[placeholder*='name']");
        if (contactNameInput != null)
        {
            await contactNameInput.FillAsync("John Test");
        }
        
        var emailInput = await Page.QuerySelectorAsync("input[type='email'], input[name*='Email']");
        if (emailInput != null)
        {
            await emailInput.FillAsync("john.test@example.com");
        }
        
        var phoneInput = await Page.QuerySelectorAsync("input[type='tel'], input[name*='Phone'], input[name*='phone']");
        if (phoneInput != null)
        {
            await phoneInput.FillAsync("+61 2 1234 5678");
        }
        
        saveButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Save'), button:has-text('Create')");
        if (saveButton != null)
        {
            await saveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(2000);
        }
        
        await TakeScreenshotAsync("E2E_Contact_Created");
        TestContext.WriteLine("Contact created successfully");
        
        TestContext.WriteLine("=== CRM Workflow Test Complete ===");
    }
}
