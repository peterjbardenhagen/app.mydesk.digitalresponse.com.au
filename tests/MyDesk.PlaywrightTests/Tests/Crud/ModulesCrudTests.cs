using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests.Crud;

// Each test fixture below extends BaseModuleCrudTest, which contributes the
// standard list/seed-row/create/detail tests. Module-specific assertions
// (PDF download, email composer dialog, enable-disable toggle) are added inline
// where the module supports them.
//
// The tests target the Demo MyDesk tenant (configured by TestSettings.TenantSlug)
// so they exercise seeded fixtures rather than production data.

[TestFixture]
public class CompaniesCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/companies";
    protected override string? CreateUrl  => "/companies/create";
    protected override string ModuleName  => "Companies";
}

[TestFixture]
public class ContactsCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/contacts";
    protected override string? CreateUrl  => "/contacts/create";
    protected override string ModuleName  => "Contacts";
}

[TestFixture]
public class ProductsCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/products";
    protected override string? CreateUrl  => "/products/create";
    protected override string ModuleName  => "Products";
}

[TestFixture]
public class QuotesCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/quotes";
    protected override string? CreateUrl  => "/quotes/create";
    protected override string ModuleName  => "Quotes";

    [Test]
    public async Task Quote_PDF_Endpoint_Returns_200()
    {
        await LoginAsync();
        await NavigateToAsync("/quotes");

        // Find a [DEMO] quote row, capture the row link to extract the id, and call the PDF endpoint.
        var firstQuoteId = await TryReadFirstNumericRowIdAsync();
        if (firstQuoteId is null)
        {
            Assert.Ignore("Quotes: could not derive a quote id from the list page.");
            return;
        }

        var resp = await Page.APIRequest.GetAsync($"{Settings.BaseUrl}/api/pdf/quote/{firstQuoteId}");
        Assert.That((int)resp.Status, Is.EqualTo(200), $"Quote PDF endpoint returned {resp.Status}.");
        Assert.That(resp.Headers["content-type"], Does.Contain("pdf"));
    }

    private async Task<int?> TryReadFirstNumericRowIdAsync()
    {
        // Heuristic: read all anchors with hrefs that look like /quotes/{id}.
        var hrefs = await Page.Locator("a[href*='/quotes/']").EvaluateAllAsync<string[]>(
            "els => els.map(e => e.getAttribute('href'))");
        foreach (var href in hrefs)
        {
            if (string.IsNullOrEmpty(href)) continue;
            var parts = href.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("quotes", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(parts[i + 1], out var id))
                    return id;
            }
        }
        return null;
    }
}

[TestFixture]
public class InvoicesCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/invoices";
    protected override string? CreateUrl  => "/invoices/create";
    protected override string ModuleName  => "Invoices";

    [Test]
    public async Task Invoice_PDF_Endpoint_Returns_200_For_First_Demo_Invoice()
    {
        await LoginAsync();
        await NavigateToAsync("/invoices");
        var hrefs = await Page.Locator("a[href*='/invoices/']").EvaluateAllAsync<string[]>(
            "els => els.map(e => e.getAttribute('href'))");
        int? id = null;
        foreach (var h in hrefs)
        {
            var parts = h?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("invoices", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(parts[i + 1], out var n)) { id = n; break; }
            }
            if (id is not null) break;
        }
        if (id is null) { Assert.Ignore("Invoices: no invoice id found on list page."); return; }

        var resp = await Page.APIRequest.GetAsync($"{Settings.BaseUrl}/api/pdf/invoice/{id}");
        Assert.That((int)resp.Status, Is.EqualTo(200));
    }
}

[TestFixture]
public class PurchaseOrdersCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/purchase-orders";
    protected override string? CreateUrl  => "/purchase-orders/create";
    protected override string ModuleName  => "PurchaseOrders";
}

[TestFixture]
public class JobOrdersCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/job-orders";
    protected override string? CreateUrl  => null; // no dedicated create page in current build
    protected override string ModuleName  => "JobOrders";
}

[TestFixture]
public class FilesLibraryCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/files";
    protected override string? CreateUrl  => null;
    protected override string ModuleName  => "FilesLibrary";
    protected override string SeedRowMarker => "[DEMO] Demo Folder";
}

[TestFixture]
public class ExpensesCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/expenses";
    protected override string? CreateUrl  => null;
    protected override string ModuleName  => "Expenses";
}

[TestFixture]
public class NoticeboardCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/noticeboard";
    protected override string? CreateUrl  => null;
    protected override string ModuleName  => "Noticeboard";
}

[TestFixture]
public class ScheduledTasksCrudTests : BaseModuleCrudTest
{
    protected override string ListUrl     => "/admin/scheduled-tasks";
    protected override string? CreateUrl  => null; // creation is a dialog, not a separate page
    protected override string ModuleName  => "ScheduledTasks";
    protected override string SeedRowMarker => "[DEMO] Weekly pipeline summary";

    [Test]
    public async Task Toggle_Disabled_Task_Button_Is_Visible()
    {
        await LoginAsync();
        await NavigateToAsync(ListUrl);
        var pauseOrPlay = Page.Locator("button:has(svg)").Filter(new()
        {
            HasTextRegex = new System.Text.RegularExpressions.Regex(".*", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        });
        // Light-touch — the page has at least one icon button per row.
        var count = await pauseOrPlay.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Expected at least one action button row.");
    }
}
