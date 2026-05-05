using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Seeds the Demo MyDesk tenant (<see cref="TenantConstants.DemoTenantId"/>) with
/// realistic-but-clearly-marked test data so Playwright tests, demos and screenshots
/// have something to work with.
///
/// Behaviour:
///   * <b>Idempotent</b> — a sentinel check (one Company row whose name starts with
///     "[DEMO]") is used to skip if seeding has already run.
///   * Runs <i>impersonating</i> the Demo tenant via <see cref="TenantImpersonation"/>
///     so DatabaseService's SQL session context is set to the Demo TenantId.
///   * After each batch of inserts, the rows are tagged with <c>TenantId = DemoTenantId</c>
///     so they are properly scoped when row-level filtering is enabled later.
///   * All names / codes are prefixed with <c>[DEMO]</c> or <c>DEMO-</c> so they're
///     visually obvious — even if a non-Demo user somehow sees them.
///   * All seeded contact emails route through the Demo tenant's email-redirect guard
///     (everything ends up at peter@bardenhagen.xyz when emails are sent).
///
/// Hosted as a <see cref="Microsoft.Extensions.Hosting.IHostedService"/> via
/// <c>Program.cs</c>, runs once on startup after database tables are verified.
/// </summary>
public class DemoDataSeeder
{
    private const string Sentinel = "[DEMO]";
    private const string SeedUserCode = "TL0025";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(IServiceScopeFactory scopeFactory, ILogger<DemoDataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        using var _ = TenantImpersonation.For(TenantConstants.DemoTenantId, "Demo MyDesk", null, SeedUserCode);
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<DatabaseService>();

        if (await IsAlreadySeeded(db))
        {
            _logger.LogInformation("Demo MyDesk: seed data already present — skipping.");
            return;
        }

        // Clean up any partial/orphaned demo data from previous failed runs before seeding.
        await CleanupPartialDemoDataAsync(db);

        _logger.LogInformation("Demo MyDesk: seeding test data…");
        try
        {
            // Order matters — companies first (FK target for contacts, quotes, invoices, etc.).
            var companyIds = await SeedCompaniesAsync(sp, db);
            var contactIds = await SeedContactsAsync(sp, db, companyIds);
            var productIds = await SeedProductsAsync(sp, db);
            await SeedQuotesAsync(sp, db, companyIds, contactIds);
            await SeedInvoicesAsync(sp, db, companyIds);
            await SeedPurchaseOrdersAsync(sp, db, contactIds);
            await SeedJobOrdersAsync(sp, db, companyIds, contactIds);
            await SeedExpensesAsync(sp, db);
            await SeedBankingAsync(sp, db);
            await SeedNoticesAsync(sp, db);
            await SeedFilesAsync(sp);
            await SeedScheduledTasksAsync(sp);

            _logger.LogInformation("Demo MyDesk: seeding complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo MyDesk: seeding failed (other tenants unaffected).");
        }
    }

    /// <summary>
    /// Removes any partial/orphaned demo data from previous failed seeding attempts.
    /// This ensures a clean slate before each seed run.
    /// </summary>
    private async Task CleanupPartialDemoDataAsync(DatabaseService db)
    {
        try
        {
            var pat = "\\[DEMO\\]%";
            
            // Delete in reverse dependency order
            await db.ExecuteNonQueryAsync("DELETE FROM ScheduledTasks WHERE Name LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM FileLibrary WHERE Name LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM Noticeboard WHERE Title LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            
            // Banking: delete transactions first, then statements
            await db.ExecuteNonQueryAsync(@"
                DELETE FROM BankTransactions 
                WHERE BankStatementId IN (SELECT BankStatementId FROM BankStatements WHERE AccountName LIKE @Pat ESCAPE '\\')",
                new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM BankStatements WHERE AccountName LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            
            await db.ExecuteNonQueryAsync("DELETE FROM Expenses WHERE Description LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM JobOrders WHERE Notes LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM PurchaseOrders WHERE Project LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM Invoices WHERE CustomerPO LIKE 'DEMO-PO-%'", new());
            await db.ExecuteNonQueryAsync("DELETE FROM Quotes WHERE Reference LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM Products WHERE ProductName LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM Contacts WHERE Email LIKE @Pat ESCAPE '\\' OR FirstName LIKE @Pat ESCAPE '\\' OR Surname LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
            await db.ExecuteNonQueryAsync("DELETE FROM Companies WHERE Company LIKE @Pat ESCAPE '\\'", new() { ["Pat"] = pat });
        }
        catch (Exception ex)
        {
            // Log but don't fail - the seeder will attempt to create new data anyway.
            _logger.LogDebug(ex, "Demo seed: cleanup of partial data skipped (tables may not exist yet).");
        }
    }

    private static async Task<bool> IsAlreadySeeded(DatabaseService db)
    {
        try
        {
            // Check multiple sentinel markers to ensure seeding is truly complete.
            // Use ESCAPE '\' to treat [DEMO] as literal string, not LIKE character class.
            var pat = "\\[DEMO\\]%";
            
            var companies = await db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Companies WHERE Company LIKE @Pat ESCAPE '\\'",
                new() { ["Pat"] = pat });
            
            if (companies > 0) return true;
            
            // If no companies but we have demo quotes, consider it seeded (partial seed).
            var quotes = await db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE Reference LIKE @Pat ESCAPE '\\'",
                new() { ["Pat"] = pat });
            
            return quotes > 0;
        }
        catch
        {
            // If tables don't exist yet, assume not seeded.
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Modules
    // ──────────────────────────────────────────────────────────────────────

    private async Task<List<int>> SeedCompaniesAsync(IServiceProvider sp, DatabaseService db)
    {
        var svc = sp.GetRequiredService<CompanyService>();
        var ids = new List<int>();

        var samples = new[]
        {
            new Company { CompanyName = $"{Sentinel} Acme Engineering",      Address1 = "12 Sample St",  Suburb = "Brisbane", State = "QLD", PostCode = "4000", Phone = "07 0000 0001", Email = "demo+acme@bardenhagen.xyz",      Website = "https://example.com/acme",     ABN = "11 111 111 111", IsCustomer = true,  IsSupplier = false, Notes = "Demo customer — fixture for Playwright tests." },
            new Company { CompanyName = $"{Sentinel} Brightlight Studios",    Address1 = "44 Demo Ave",   Suburb = "Sydney",   State = "NSW", PostCode = "2000", Phone = "02 0000 0002", Email = "demo+brightlight@bardenhagen.xyz", Website = "https://example.com/brightlight", ABN = "22 222 222 222", IsCustomer = true,  IsSupplier = false, Notes = "Demo customer." },
            new Company { CompanyName = $"{Sentinel} Coastal Constructions", Address1 = "9 Sandbar Rd",  Suburb = "Gold Coast", State = "QLD", PostCode = "4217", Phone = "07 0000 0003", Email = "demo+coastal@bardenhagen.xyz",   Website = "https://example.com/coastal",    ABN = "33 333 333 333", IsCustomer = true,  IsSupplier = false, Notes = "Demo customer with multiple jobs." },
            new Company { CompanyName = $"{Sentinel} Northwind Wholesale",   Address1 = "1 Trade St",    Suburb = "Melbourne",State = "VIC", PostCode = "3000", Phone = "03 0000 0004", Email = "demo+northwind@bardenhagen.xyz", Website = "https://example.com/northwind",  ABN = "44 444 444 444", IsCustomer = false, IsSupplier = true,  Notes = "Demo supplier — used for purchase orders." },
            new Company { CompanyName = $"{Sentinel} Voltage Supplies",      Address1 = "8 Power Pde",   Suburb = "Adelaide", State = "SA",  PostCode = "5000", Phone = "08 0000 0005", Email = "demo+voltage@bardenhagen.xyz",   Website = "https://example.com/voltage",    ABN = "55 555 555 555", IsCustomer = false, IsSupplier = true,  Notes = "Demo supplier — second supplier for PO tests." },
        };

        foreach (var c in samples)
        {
            try
            {
                var id = await svc.SaveCompanyAsync(c);
                if (id > 0) ids.Add(id);
                else _logger.LogWarning("Demo seed: Company '{Name}' returned ID 0", c.CompanyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo seed: FAILED to create Company '{Name}'", c.CompanyName);
            }
        }

        await TagTenantAsync(db, "Companies", "CompanyId", ids);
        _logger.LogInformation("Demo seed: {Count} companies", ids.Count);
        return ids;
    }

    private async Task<List<int>> SeedContactsAsync(IServiceProvider sp, DatabaseService db, List<int> companyIds)
    {
        if (companyIds.Count == 0) return new();
        var svc = sp.GetRequiredService<ContactService>();
        var ids = new List<int>();

        // Spread contacts across the customer companies (first three of the five seeded)
        var customers = companyIds.Take(3).ToList();
        var samples = new (string F, string S, string Pos, string Email, int CompanyIdx)[]
        {
            ("Alice",  "Anders",   "Operations Manager", "demo+alice@bardenhagen.xyz",  0),
            ("Bruno",  "Borden",   "Procurement Lead",   "demo+bruno@bardenhagen.xyz",  0),
            ("Cara",   "Chen",     "CEO",                "demo+cara@bardenhagen.xyz",   1),
            ("Devon",  "Diaz",     "Project Manager",    "demo+devon@bardenhagen.xyz",  1),
            ("Erin",   "Edwards",  "Financial Controller","demo+erin@bardenhagen.xyz",  2),
            ("Frank",  "Fisher",   "Site Supervisor",    "demo+frank@bardenhagen.xyz", 2),
            ("Greta",  "Gomez",    "Marketing Lead",     "demo+greta@bardenhagen.xyz", 0),
            ("Hugo",   "Hansen",   "IT Manager",         "demo+hugo@bardenhagen.xyz",  1),
        };

        foreach (var s in samples)
        {
            try
            {
                var c = new Contact
                {
                    FirstName = s.F,
                    Surname   = s.S,
                    Position  = s.Pos,
                    Email     = s.Email,
                    Phone     = "07 0000 0000",
                    Mobile    = "0400 000 000",
                    CompanyId = customers[Math.Min(s.CompanyIdx, customers.Count - 1)],
                };
                var id = await svc.CreateContactAsync(c, SeedUserCode);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Contact '{F} {S}'", s.F, s.S); }
        }

        await TagTenantAsync(db, "Contacts", "ContactId", ids);
        _logger.LogInformation("Demo seed: {Count} contacts", ids.Count);
        return ids;
    }

    private async Task<List<int>> SeedProductsAsync(IServiceProvider sp, DatabaseService db)
    {
        var svc = sp.GetRequiredService<ProductService>();
        var ids = new List<int>();

        var samples = new[]
        {
            new Product { ProductName = $"{Sentinel} LED Panel 600x600 36W", Description = "Recessed LED panel, 4000K, dimmable.",        UnitCost = 45.00m, UnitPrice = 89.00m },
            new Product { ProductName = $"{Sentinel} Track Light 20W",        Description = "Adjustable track-mounted spot, 3000K.",       UnitCost = 28.00m, UnitPrice = 64.00m },
            new Product { ProductName = $"{Sentinel} Pendant Light Brass",    Description = "Heritage brass pendant for hospitality.",     UnitCost = 110.00m,UnitPrice = 245.00m },
            new Product { ProductName = $"{Sentinel} Emergency Exit Sign",    Description = "AS/NZS 2293 compliant 90-min emergency.",    UnitCost = 32.00m, UnitPrice = 78.00m },
            new Product { ProductName = $"{Sentinel} Installation Service",   Description = "Hourly install rate (electrician).",          UnitCost = 65.00m, UnitPrice = 110.00m },
            new Product { ProductName = $"{Sentinel} Lighting Design Hour",   Description = "Designer hourly rate.",                       UnitCost = 90.00m, UnitPrice = 180.00m },
        };

        foreach (var p in samples)
        {
            try
            {
                var id = await svc.SaveAsync(p);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Product '{Name}'", p.ProductName); }
        }

        await TagTenantAsync(db, "Products", "ProductId", ids);
        _logger.LogInformation("Demo seed: {Count} products", ids.Count);
        return ids;
    }

    private async Task SeedQuotesAsync(IServiceProvider sp, DatabaseService db,
        List<int> companyIds, List<int> contactIds)
    {
        if (companyIds.Count == 0 || contactIds.Count == 0) return;
        var svc = sp.GetRequiredService<QuoteService>();
        var ids = new List<int>();

        var quotes = new[]
        {
            (Reference: $"{Sentinel} Office Refit – Stage 1", CompanyIdx: 0, ContactIdx: 0, Items: new[]
            {
                ("LED Panel 600x600 36W", 24m, 45m, 89m),
                ("Installation Service",  16m, 65m, 110m),
            }),
            (Reference: $"{Sentinel} Hotel Lobby Lighting",   CompanyIdx: 1, ContactIdx: 2, Items: new[]
            {
                ("Pendant Light Brass",   12m, 110m, 245m),
                ("Track Light 20W",       18m, 28m,  64m),
                ("Lighting Design Hour",   8m, 90m, 180m),
            }),
            (Reference: $"{Sentinel} Warehouse Compliance",   CompanyIdx: 2, ContactIdx: 4, Items: new[]
            {
                ("Emergency Exit Sign",   30m, 32m, 78m),
                ("Installation Service",  10m, 65m, 110m),
            }),
        };

        for (int i = 0; i < quotes.Length; i++)
        {
            var q = quotes[i];
            try
            {
                var quote = new Quote
                {
                    Reference   = q.Reference,
                    CompanyId   = companyIds[Math.Min(q.CompanyIdx, companyIds.Count - 1)],
                    ContactId   = contactIds[Math.Min(q.ContactIdx, contactIds.Count - 1)],
                    DivisionId  = 1,
                    Validity    = 30,
                    Attention   = "Demo Recipient",
                    Delivery    = "Site delivery",
                    Terms       = "Payment 14 days from invoice",
                    CustomerNotes = "This is a demo quote — emails will be redirected.",
                };
                var lines = q.Items.Select(it => new QuoteLineItem
                {
                    Description = it.Item1,
                    Quantity    = it.Item2,
                    UnitCost    = it.Item3,
                    NettPrice   = it.Item4,
                    ExtNettPrice = it.Item2 * it.Item4,
                    Type        = "Product",
                    Units       = 1m,
                }).ToList();

                var id = await svc.CreateQuoteAsync(quote, lines, new List<QuoteThirdPartyItem>(), SeedUserCode);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Quote '{Ref}'", q.Reference); }
        }

        await TagTenantAsync(db, "Quotes", "Qid", ids);
        // Try to tag the line items too (not all installs share the column name; ignore failures).
        await TagTenantByParentAsync(db, "QuoteContents", "Qid", ids);
        _logger.LogInformation("Demo seed: {Count} quotes", ids.Count);
    }

    private async Task SeedInvoicesAsync(IServiceProvider sp, DatabaseService db, List<int> companyIds)
    {
        if (companyIds.Count == 0) return;
        var svc = sp.GetRequiredService<InvoiceService>();
        var ids = new List<int>();

        var invoices = new[]
        {
            (CompanyIdx: 0, Co: "[DEMO] Acme Engineering",     Lines: new[]
            {
                ("Project management hours - April",          1m, 1500m),
                ("On-site commissioning",                     1m,  650m),
            }),
            (CompanyIdx: 1, Co: "[DEMO] Brightlight Studios",  Lines: new[]
            {
                ("Lighting design consultation",              4m,  180m),
                ("Travel allowance",                          1m,  120m),
            }),
        };

        foreach (var i in invoices)
        {
            try
            {
                var nett = i.Lines.Sum(l => l.Item2 * l.Item3);
                var inv = new Invoice
                {
                    CompanyId      = companyIds[Math.Min(i.CompanyIdx, companyIds.Count - 1)],
                    InvCompany     = i.Co,
                    DelCompany     = i.Co,
                    InvAddress     = "Demo Address",
                    DelAddress     = "Demo Address",
                    InvoiceDate    = DateTime.Today,
                    DivisionId     = 1,
                    Qid            = 0,
                    CustomerPO     = $"DEMO-PO-{Random.Shared.Next(1000, 9999)}",
                    Attention      = "Demo Recipient",
                    Account        = "DEMO",
                    Terms          = "14 days",
                    CustomerNotes  = "Demo invoice — redirected email only.",
                    NettPriceTotal = nett,
                    GSTTotal       = nett * 0.10m,
                };
                var lines = i.Lines.Select(l => new InvoiceLineItem
                {
                    Description  = l.Item1,
                    Quantity     = l.Item2,
                    NettPrice    = l.Item3,
                    ExtNettPrice = l.Item2 * l.Item3,
                }).ToList();

                var id = await svc.CreateInvoiceAsync(inv, lines, SeedUserCode);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Invoice for '{Co}'", i.Co); }
        }

        await TagTenantAsync(db, "Invoices", "InvoiceId", ids);
        await TagTenantByParentAsync(db, "InvoiceContents", "InvoiceId", ids);
        _logger.LogInformation("Demo seed: {Count} invoices", ids.Count);
    }

    private async Task SeedPurchaseOrdersAsync(IServiceProvider sp, DatabaseService db, List<int> contactIds)
    {
        if (contactIds.Count == 0) return;
        var svc = sp.GetRequiredService<PurchaseOrderService>();
        var ids = new List<int>();

        var pos = new[]
        {
            (Project: "[DEMO] Office Refit – Stage 1", ContactIdx: 0, Lines: new[]
            {
                ("LED Panel 600x600 36W", 24m, 45m),
                ("Track Light 20W",       12m, 28m),
            }),
            (Project: "[DEMO] Warehouse Compliance",   ContactIdx: 1, Lines: new[]
            {
                ("Emergency Exit Sign",   30m, 32m),
                ("Installation kit",       1m, 220m),
            }),
        };

        foreach (var p in pos)
        {
            try
            {
                var ex = p.Lines.Sum(l => l.Item2 * l.Item3);
                var po = new PurchaseOrder
                {
                    Project              = p.Project,
                    ContactId            = contactIds[Math.Min(p.ContactIdx, contactIds.Count - 1)],
                    DivisionId           = 1,
                    PODate               = DateTime.Today,
                    DateRequired         = DateTime.Today.AddDays(14),
                    GST                  = true,
                    POPaymentTypeId      = 1,
                    Terms                = "30 days",
                    DeliverToLocation    = "Demo Warehouse, Brisbane",
                    IntroText            = "Demo purchase order — please ignore.",
                    InternalNotes        = "Auto-created by DemoDataSeeder.",
                    PriceExTotal         = ex,
                    PriceGSTTotal        = ex * 0.10m,
                    PriceIncTotal        = ex * 1.10m,
                };
                var lines = p.Lines.Select(l => new POLineItem
                {
                    Description     = l.Item1,
                    Quantity        = (int)l.Item2,
                    PriceEx         = l.Item3,
                    PriceExSubTotal = l.Item2 * l.Item3,
                }).ToList();

                var id = await svc.CreatePurchaseOrderAsync(po, lines, SeedUserCode);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create PO '{Project}'", p.Project); }
        }

        await TagTenantAsync(db, "PurchaseOrders", "POid", ids);
        await TagTenantByParentAsync(db, "PurchaseOrderContents", "POid", ids);
        _logger.LogInformation("Demo seed: {Count} purchase orders", ids.Count);
    }

    private async Task SeedJobOrdersAsync(IServiceProvider sp, DatabaseService db,
        List<int> companyIds, List<int> contactIds)
    {
        if (companyIds.Count == 0 || contactIds.Count == 0) return;
        var svc = sp.GetRequiredService<JobOrderService>();
        var ids = new List<int>();

        var jobs = new[]
        {
            (Notes: "[DEMO] Job order for Acme office refit",     CompanyIdx: 0, ContactIdx: 0, Lines: new[]
            {
                ("Site survey",                  4m, 720m),
                ("Equipment delivery",           1m, 480m),
            }),
            (Notes: "[DEMO] Coastal warehouse install",            CompanyIdx: 2, ContactIdx: 4, Lines: new[]
            {
                ("Install emergency lighting",  30m, 2400m),
                ("Compliance certificate",       1m,  300m),
            }),
        };

        foreach (var j in jobs)
        {
            try
            {
                var jo = new JobOrderDetail
                {
                    JobOrderDate     = DateTime.Today,
                    DivisionId       = 1,
                    CompanyId        = companyIds[Math.Min(j.CompanyIdx, companyIds.Count - 1)],
                    ContactId        = contactIds[Math.Min(j.ContactIdx, contactIds.Count - 1)],
                    JobOrderStatusId = 1,
                    Notes            = j.Notes,
                };
                var lines = j.Lines.Select(l => new JobOrderLineItem
                {
                    Description = l.Item1,
                    Qty         = (int)l.Item2,
                    LineTotal   = l.Item3,
                }).ToList();

                var id = await svc.SaveAsync(jo, lines, SeedUserCode);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create JobOrder '{Notes}'", j.Notes); }
        }

        await TagTenantAsync(db, "JobOrders", "JobOrderId", ids);
        await TagTenantByParentAsync(db, "JobOrderContents", "JobOrderId", ids);
        _logger.LogInformation("Demo seed: {Count} job orders", ids.Count);
    }

    private async Task SeedExpensesAsync(IServiceProvider sp, DatabaseService db)
    {
        var svc = sp.GetRequiredService<ExpenseService>();
        try { await svc.EnsureTableAsync(); } catch { /* table may already exist */ }
        var ids = new List<int>();

        // Mix of AUD (with GST) and USD (no GST, AUD-equivalent recorded) so the
        // multi-currency UI has examples to render.
        var samples = new[]
        {
            new Expense { Date = DateTime.Today.AddDays(-7), Description = "[DEMO] Site fuel",       Amount = 95m,  Currency = "AUD", HasGst = true,  Total = 95m,  AmountAud = 95m,
                          SupplierName = "[DEMO] BP Spring Hill",   Category = "Fuel",          Status = "Approved", CreatedBy = SeedUserCode },
            new Expense { Date = DateTime.Today.AddDays(-3), Description = "[DEMO] Client lunch",    Amount = 124m, Currency = "AUD", HasGst = true,  Total = 124m, AmountAud = 124m,
                          SupplierName = "[DEMO] Coffee Club CBD",  Category = "Entertainment", Status = "Pending",  CreatedBy = SeedUserCode },
            new Expense { Date = DateTime.Today.AddDays(-1), Description = "[DEMO] Office supplies", Amount = 47m,  Currency = "AUD", HasGst = true,  Total = 47m,  AmountAud = 47m,
                          SupplierName = "[DEMO] Officeworks",      Category = "Stationery",    Status = "Approved", CreatedBy = SeedUserCode },
            new Expense { Date = DateTime.Today.AddDays(-5), Description = "[DEMO] OpenAI API credit",
                          Amount = 200m, Currency = "USD", HasGst = false, Total = 200m,
                          ExchangeRate = 0.658m, AmountAud = 304m, AmountAudSource = "receipt",
                          SupplierName = "[DEMO] OpenAI",           Category = "Software",      Status = "Approved", CreatedBy = SeedUserCode },
            new Expense { Date = DateTime.Today.AddDays(-12), Description = "[DEMO] AWS hosting",
                          Amount = 87.50m, Currency = "USD", HasGst = false, Total = 87.50m,
                          ExchangeRate = 0.66m, AmountAud = 132.58m, AmountAudSource = "bank",
                          SupplierName = "[DEMO] Amazon Web Services", Category = "Software",   Status = "Approved", CreatedBy = SeedUserCode },
        };

        foreach (var e in samples)
        {
            try
            {
                var id = await svc.SaveExpenseAsync(e);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Expense '{Desc}'", e.Description); }
        }

        await TagTenantAsync(db, "Expenses", "ExpenseId", ids);
        _logger.LogInformation("Demo seed: {Count} expenses", ids.Count);
    }

    private async Task SeedBankingAsync(IServiceProvider sp, DatabaseService db)
    {
        var svc = sp.GetService<BankingService>();
        if (svc is null) return;
        try { await svc.EnsureTablesAsync(); } catch { /* may already exist */ }

        // Skip if already seeded.
        try
        {
            var existing = await db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM BankStatements WHERE AccountName LIKE @Pat",
                new() { ["Pat"] = $"{Sentinel}%" });
            if (existing > 0) return;
        }
        catch { /* table may not exist yet */ }

        var today = DateTime.Today;
        var stmt = new BankStatement
        {
            AccountName     = $"{Sentinel} CBA Business Account",
            Bsb             = "062-000",
            AccountNumber   = "1234",
            Currency        = "AUD",
            FromDate        = today.AddDays(-30),
            ToDate          = today,
            OpeningBalance  = 12_500.00m,
            Source          = "manual-csv",
            FileName        = "demo-statement.csv",
            UploadedBy      = SeedUserCode,
        };
        try
        {
            var stmtId = await svc.CreateStatementAsync(stmt);

            // Five demo transactions — mix of credits (customer payments) and debits (supplier
            // payments + the two demo USD expenses).
            var txns = new (DateTime Date, string Desc, string? Ref, decimal Debit, decimal Credit)[]
            {
                (today.AddDays(-25), "[DEMO] Customer payment - Acme Engineering", "INV0001",   0m,    1_650.00m),
                (today.AddDays(-22), "[DEMO] Supplier payment - Northwind",         "PO-1014",  580.00m, 0m),
                (today.AddDays(-12), "[DEMO] Card - Amazon Web Services USD 87.50", null,        132.58m, 0m),
                (today.AddDays(-7),  "[DEMO] Card - BP Spring Hill",                "FUEL",      95.00m,  0m),
                (today.AddDays(-5),  "[DEMO] Card - OpenAI USD 200",                null,        304.00m, 0m),
                (today.AddDays(-3),  "[DEMO] Customer payment - Brightlight",       "INV0002",   0m,      720.00m),
            };
            decimal running = stmt.OpeningBalance;
            foreach (var t in txns)
            {
                running += t.Credit - t.Debit;
                await svc.AddTransactionAsync(new BankTransaction
                {
                    BankStatementId = stmtId,
                    TransactionDate = t.Date,
                    Description     = t.Desc,
                    Reference       = t.Ref,
                    Debit           = t.Debit,
                    Credit          = t.Credit,
                    Balance         = running,
                });
            }

            // Update statement closing balance + count.
            await db.ExecuteNonQueryAsync(
                @"UPDATE BankStatements SET ClosingBalance = @C, TransactionCount = @N WHERE BankStatementId = @Id",
                new() { ["C"] = running, ["N"] = txns.Length, ["Id"] = stmtId });

            _logger.LogInformation("Demo seed: 1 bank statement + {Count} transactions", txns.Length);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to seed Banking."); }
    }

    private async Task SeedNoticesAsync(IServiceProvider sp, DatabaseService db)
    {
        var svc = sp.GetRequiredService<NoticeboardService>();
        try { await svc.EnsureTableAsync(); } catch { /* may already exist */ }
        var ids = new List<int>();

        var samples = new[]
        {
            new Notice { Title = "[DEMO] Welcome to Demo MyDesk",     Body = "This is a sandbox tenant. All emails are redirected to peter@bardenhagen.xyz.", PostedBy = SeedUserCode, ExpiryDate = DateTime.Today.AddDays(90) },
            new Notice { Title = "[DEMO] Quarterly all-hands meeting", Body = "Friday 2pm in the main meeting room. Lunch provided.",                          PostedBy = SeedUserCode, ExpiryDate = DateTime.Today.AddDays(14) },
        };

        foreach (var n in samples)
        {
            try
            {
                var id = await svc.SaveAsync(n);
                if (id > 0) ids.Add(id);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create Notice '{Title}'", n.Title); }
        }

        await TagTenantAsync(db, "Noticeboard", "NoticeboardId", ids);
        _logger.LogInformation("Demo seed: {Count} notices", ids.Count);
    }

    private async Task SeedFilesAsync(IServiceProvider sp)
    {
        var svc = sp.GetService<FileLibraryService>();
        if (svc is null) return;
        try
        {
            var rootId = await svc.CreateFolderAsync($"{Sentinel} Demo Folder", null, null, SeedUserCode);
            _ = await svc.CreateFolderAsync($"{Sentinel} Quotes", rootId, null, SeedUserCode);
            _ = await svc.CreateFolderAsync($"{Sentinel} Photos", rootId, null, SeedUserCode);

            var meta = new FileLibraryItem
            {
                FileId = Guid.NewGuid(),
                Name = $"{Sentinel} Welcome.txt",
                IsFolder = false,
                ParentFolderId = rootId,
                FilePath = "demo/welcome.txt",
                FileSize = 42,
                ContentType = "text/plain",
                IsPublic = false,
                CreatedBy = SeedUserCode,
            };
            await svc.SaveItemAsync(meta, SeedUserCode);
            _logger.LogInformation("Demo seed: file library structure created.");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to seed FileLibrary."); }
    }

    private async Task SeedScheduledTasksAsync(IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<ScheduledTaskService>();
        try { await svc.EnsureTablesAsync(); } catch { /* may already exist */ }

        var samples = new[]
        {
            new ScheduledTask
            {
                Name = $"{Sentinel} Weekly pipeline summary",
                Description = "AI-generated summary of the week's quotes & invoices, emailed to the Demo address.",
                ActionType = nameof(ScheduledTaskActionType.AskAi),
                Recurrence = nameof(ScheduleRecurrence.Weekly),
                DayOfWeek = 1, // Monday
                HourOfDay = 9,
                MinuteOfHour = 0,
                IsEnabled = true,
                CreatedBy = SeedUserCode,
                ParametersJson =
                    "{\"prompt\":\"Summarise this week's MyDesk activity in <=200 words. " +
                    "Cover new quotes, paid invoices, outstanding POs, and any flags worth noting.\"," +
                    "\"emailTo\":\"peter@bardenhagen.xyz\"," +
                    "\"subject\":\"[Demo] Weekly pipeline summary\"," +
                    "\"maxTokens\":600}"
            },
            new ScheduledTask
            {
                Name = $"{Sentinel} Daily reminder",
                Description = "Sends a templated email reminder every weekday morning.",
                ActionType = nameof(ScheduledTaskActionType.SendEmail),
                Recurrence = nameof(ScheduleRecurrence.Daily),
                HourOfDay = 8,
                MinuteOfHour = 30,
                IsEnabled = false, // disabled by default so it doesn't actually fire on first install
                CreatedBy = SeedUserCode,
                ParametersJson =
                    "{\"to\":\"peter@bardenhagen.xyz\"," +
                    "\"subject\":\"[Demo] Don't forget timesheets\"," +
                    "\"body\":\"<p>Friendly reminder to file your timesheet.</p>\"}"
            },
        };

        foreach (var t in samples)
        {
            try { await svc.CreateAsync(t); }
            catch (Exception ex) { _logger.LogWarning(ex, "Demo seed: failed to create ScheduledTask '{Name}'", t.Name); }
        }

        _logger.LogInformation("Demo seed: 2 scheduled tasks (1 enabled, 1 disabled).");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Update the TenantId column on freshly-inserted rows so they're scoped to Demo.</summary>
    private async Task TagTenantAsync(DatabaseService db, string table, string idColumn, List<int> ids)
    {
        if (ids.Count == 0) return;
        try
        {
            // Only run if the table actually has a TenantId column (added by migration 018+).
            var hasTenantId = await db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @T AND COLUMN_NAME = 'TenantId'",
                new() { ["T"] = table });
            if (hasTenantId == 0) return;

            var idList = string.Join(",", ids);
            var sql = $"UPDATE {table} SET TenantId = @TenantId WHERE {idColumn} IN ({idList})";
            await db.ExecuteNonQueryAsync(sql, new() { ["TenantId"] = TenantConstants.DemoTenantId });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Demo seed: TagTenantAsync skipped for {Table}", table);
        }
    }

    /// <summary>Update TenantId on child-table rows whose parent FK is in the supplied id list.</summary>
    private async Task TagTenantByParentAsync(DatabaseService db, string childTable, string parentIdColumn, List<int> parentIds)
    {
        if (parentIds.Count == 0) return;
        try
        {
            var hasTenantId = await db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @T AND COLUMN_NAME = 'TenantId'",
                new() { ["T"] = childTable });
            if (hasTenantId == 0) return;

            var idList = string.Join(",", parentIds);
            var sql = $"UPDATE {childTable} SET TenantId = @TenantId WHERE {parentIdColumn} IN ({idList})";
            await db.ExecuteNonQueryAsync(sql, new() { ["TenantId"] = TenantConstants.DemoTenantId });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Demo seed: TagTenantByParentAsync skipped for {Table}", childTable);
        }
    }
}
