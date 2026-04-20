using Techlight.MyDesk.Shared.Models;
using Techlight.MyDesk.Shared.Services;

namespace Techlight.MyDesk.Web.Services;

public class QuoteService { }
public class InvoiceService { }
public class PurchaseOrderService { }
public class ContactService { }
public class CompanyService { }

public class DashboardService 
{
    private readonly DatabaseService _db;
    public DashboardService(DatabaseService db) => _db = db;

    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        // To be implemented fully with DB queries
        return new DashboardMetrics();
    }
}

public class LookupService { }
