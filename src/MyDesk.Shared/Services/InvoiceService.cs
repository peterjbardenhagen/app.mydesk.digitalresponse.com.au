using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class InvoiceService
{
    private readonly DatabaseService _db;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(DatabaseService db, ILogger<InvoiceService> logger)
    {
        _db       = db;
        _logger   = logger;
    }

    // ── Shared SELECT fragment ──────────────────────────────────────────────
    private const string SelectCols = @"
        i.InvoiceId,
        CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNum,
        i.InvoiceDate,
        ISNULL(i.Code, '')         AS Code,
        ISNULL(u.Name, '')         AS InvoicedBy,
        ISNULL(i.InvoiceStatusId,1) AS InvoiceStatusId,
        ISNULL(s.InvoiceStatus,'') AS StatusName,
        ISNULL(i.DivisionId, 0)    AS DivisionId,
        ISNULL(d.Division,'')      AS DivisionName,
        ISNULL(i.Qid, 0)           AS Qid,
        ISNULL(i.CompanyId, 0)     AS CompanyId,
        NULL                       AS ContactId,
        ISNULL(co.Company, '')     AS CCompany,
        ISNULL(i.InvCompany,'')    AS InvCompany,
        ISNULL(i.DelCompany,'')    AS DelCompany,
        ISNULL(i.InvAddress,'')    AS InvAddress,
        ISNULL(i.DelAddress,'')    AS DelAddress,
        i.CustomerPO, i.Attention, i.Account, i.Terms,
        i.CustomerNotes, i.InternalNotes,
        ISNULL(i.NettPriceTotal, 0) AS NettPriceTotal,
        ISNULL(i.GSTTotal, 0)       AS GSTTotal,
        ISNULL(i.ExportedToMYOB, 0) AS ExportedToMYOB,
        i.ExportedDate
    FROM Invoices i
    LEFT JOIN Users u        ON i.Code = u.Code
    LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId
    LEFT JOIN Divisions d    ON i.DivisionId = d.DivisionId
    LEFT JOIN Companies co   ON i.CompanyId  = co.CompanyId";

    // ── List ───────────────────────────────────────────────────────────────

    public async Task<List<Invoice>> GetInvoicesAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? customer = null, int statusId = 0,
        string? originatorCode = null, int? divisionId = null, int limit = 500)
    {
        var sql = $"SELECT TOP {limit} {SelectCols} WHERE 1=1";
        var p   = new Dictionary<string, object?>();

        if (statusId == 555)      { sql += " AND i.InvoiceStatusId NOT IN (2,3,4,5)"; }
        else if (statusId > 0)    { sql += " AND i.InvoiceStatusId = @Sid"; p["Sid"] = statusId; }
        if (dateFrom.HasValue)    { sql += " AND i.InvoiceDate >= @F"; p["F"] = dateFrom.Value; }
        if (dateTo.HasValue)      { sql += " AND i.InvoiceDate <= @T"; p["T"] = dateTo.Value; }
        if (!string.IsNullOrEmpty(customer))       { sql += " AND (co.Company LIKE @C OR i.CCompany LIKE @C)"; p["C"] = $"%{customer}%"; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND i.Code = @OC"; p["OC"] = originatorCode; }
        if (divisionId.HasValue)  { sql += " AND i.DivisionId = @DivId"; p["DivId"] = divisionId.Value; }

        sql += " ORDER BY i.InvoiceDate DESC, i.InvoiceId DESC";
        return (await _db.QueryAsync(sql, p)).Map(MapInvoice);
    }

    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        var sql = $"SELECT {SelectCols} WHERE i.InvoiceId = @Id";
        var dt  = await _db.QueryAsync(sql, new() { ["Id"] = invoiceId });
        return dt.Rows.Count == 0 ? null : MapInvoice(dt.Rows[0]);
    }

    // ── Line items ─────────────────────────────────────────────────────────

    public async Task<List<InvoiceLineItem>> GetLineItemsAsync(int invoiceId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT InvoiceItemId, InvoiceId,
                   ISNULL(Quantity, 0)     AS Quantity,
                   ISNULL(ProductCode, '') AS ProductCode,
                   ISNULL(Description, '') AS Description,
                   ISNULL(NettPrice, 0)    AS NettPrice,
                   ISNULL(ExtNettPrice,0)  AS ExtNettPrice
            FROM InvoiceContents
            WHERE InvoiceId = @Id
            ORDER BY InvoiceItemId",
            new() { ["Id"] = invoiceId });
        return dt.Map(r => new InvoiceLineItem
        {
            InvoiceContentId = Convert.ToInt32(r["InvoiceItemId"]),
            InvoiceId   = Convert.ToInt32(r["InvoiceId"]),
            Quantity    = Convert.ToDecimal(r["Quantity"]),
            ProductCode = r["ProductCode"]?.ToString(),
            Description = r["Description"]?.ToString() ?? "",
            NettPrice   = Convert.ToDecimal(r["NettPrice"]),
            ExtNettPrice= Convert.ToDecimal(r["ExtNettPrice"]),
        });
    }

    // ── Create ─────────────────────────────────────────────────────────────

    public async Task<int> CreateInvoiceAsync(Invoice inv, List<InvoiceLineItem> lines, string userCode)
    {
        var id = await _db.InsertAsync(@"
            INSERT INTO Invoices (InvoiceDate, Code, InvoiceStatusId, DivisionId,
                Qid, CompanyId, InvCompany, DelCompany,
                InvAddress, DelAddress, CustomerPO, Attention, Account,
                Terms, CustomerNotes, InternalNotes, NettPriceTotal, GSTTotal)
            VALUES (@InvoiceDate, @Code, 1, @DivisionId,
                @Qid, @CompanyId, @InvCompany, @DelCompany,
                @InvAddress, @DelAddress, @CustomerPO, @Attention, @Account,
                @Terms, @CustomerNotes, @InternalNotes, @NettPriceTotal, @GSTTotal)",
            BuildParams(inv, userCode));

        foreach (var line in lines.Where(l => l.Quantity > 0))
            await InsertLineAsync(id, line);

        await WriteAuditAsync(id, userCode, "Invoice created");
        return id;
    }

    // ── Update ─────────────────────────────────────────────────────────────

    public async Task UpdateInvoiceAsync(Invoice inv, List<InvoiceLineItem> lines, string userCode)
    {
        var p = BuildParams(inv, userCode);
        p["InvoiceId"] = inv.InvoiceId;
        await _db.ExecuteAsync(@"
            UPDATE Invoices SET
                Code = @Code, DivisionId = @DivisionId,
                Qid = @Qid, CompanyId = @CompanyId,
                InvCompany = @InvCompany, DelCompany = @DelCompany,
                InvAddress = @InvAddress, DelAddress = @DelAddress,
                CustomerPO = @CustomerPO, Attention = @Attention, Account = @Account,
                Terms = @Terms, CustomerNotes = @CustomerNotes, InternalNotes = @InternalNotes,
                NettPriceTotal = @NettPriceTotal, GSTTotal = @GSTTotal
            WHERE InvoiceId = @InvoiceId", p);

        await _db.ExecuteAsync("DELETE FROM InvoiceContents WHERE InvoiceId = @Id",
            new() { ["Id"] = inv.InvoiceId });
        foreach (var line in lines.Where(l => l.Quantity > 0))
            await InsertLineAsync(inv.InvoiceId, line);

        await WriteAuditAsync(inv.InvoiceId, userCode, "Invoice updated");
    }

    // ── Status ─────────────────────────────────────────────────────────────

    public async Task UpdateStatusAsync(int invoiceId, int statusId, string userCode, string statusName)
    {
        await _db.ExecuteAsync(
            "UPDATE Invoices SET InvoiceStatusId = @S WHERE InvoiceId = @Id",
            new() { ["S"] = statusId, ["Id"] = invoiceId });
        await WriteAuditAsync(invoiceId, userCode, $"Status changed to {statusName}");
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public async Task DeleteAsync(int invoiceId, string userCode)
    {
        await _db.ExecuteAsync("DELETE FROM InvoiceContents WHERE InvoiceId = @Id",  new() { ["Id"] = invoiceId });
        await _db.ExecuteAsync("DELETE FROM InvoiceAudit    WHERE InvoiceId = @Id",  new() { ["Id"] = invoiceId });
        await _db.ExecuteAsync("DELETE FROM Invoices        WHERE InvoiceId = @Id",  new() { ["Id"] = invoiceId });
    }

    // ── Audit ──────────────────────────────────────────────────────────────

    public async Task<List<InvoiceAuditEntry>> GetAuditAsync(int invoiceId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT ia.Code, ISNULL(u.Name,'') AS UserName, ia.Action, ia.DateEntered
            FROM InvoiceAudit ia
            LEFT JOIN Users u ON ia.Code = u.Code
            WHERE ia.InvoiceId = @Id
            ORDER BY ia.DateEntered DESC",
            new() { ["Id"] = invoiceId });
        return dt.Map(r => new InvoiceAuditEntry
        {
            Code        = r["Code"]?.ToString(),
            UserName    = r["UserName"]?.ToString(),
            Action      = r["Action"]?.ToString(),
            DateEntered = r["DateEntered"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["DateEntered"]),
        });
    }

    // ── Despatch ───────────────────────────────────────────────────────────

    public async Task<DespatchDetail?> GetDespatchAsync(int invoiceId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT DespatchId, InvoiceId, DespatchDate,
                   Carrier, CarrierRef, PackageDetails, InternalNotes
            FROM Despatch WHERE InvoiceId = @Id",
            new() { ["Id"] = invoiceId });
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new DespatchDetail
        {
            DespatchId     = Convert.ToInt32(r["DespatchId"]),
            InvoiceId      = Convert.ToInt32(r["InvoiceId"]),
            DespatchDate   = r["DespatchDate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(r["DespatchDate"]),
            Carrier        = r["Carrier"]?.ToString(),
            CarrierRef     = r["CarrierRef"]?.ToString(),
            PackageDetails = r["PackageDetails"]?.ToString(),
            InternalNotes  = r["InternalNotes"]?.ToString(),
        };
    }

    public async Task SaveDespatchAsync(DespatchDetail d, string userCode)
    {
        await _db.ExecuteAsync("DELETE FROM Despatch WHERE InvoiceId = @Id", new() { ["Id"] = d.InvoiceId });
        await _db.InsertAsync(@"
            INSERT INTO Despatch (InvoiceId, Code, DespatchDate, Carrier, CarrierRef, PackageDetails, InternalNotes)
            VALUES (@InvoiceId, @Code, @DespatchDate, @Carrier, @CarrierRef, @PackageDetails, @InternalNotes)",
            new()
            {
                ["InvoiceId"]      = d.InvoiceId,
                ["Code"]           = (object)userCode,
                ["DespatchDate"]   = d.DespatchDate,
                ["Carrier"]        = (object?)d.Carrier ?? DBNull.Value,
                ["CarrierRef"]     = (object?)d.CarrierRef ?? DBNull.Value,
                ["PackageDetails"] = (object?)d.PackageDetails ?? DBNull.Value,
                ["InternalNotes"]  = (object?)d.InternalNotes ?? DBNull.Value,
            });
        await WriteAuditAsync(d.InvoiceId, userCode, "Despatch details entered");
    }

    // ── MYOB Export ────────────────────────────────────────────────────────

    public async Task<(byte[] CsvBytes, int Count)> ExportToMYOBAsync(
        DateTime dateFrom, DateTime dateTo, string userCode)
    {
        var dt = await _db.QueryAsync(@"
            SELECT i.InvoiceId, CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNum,
                   i.InvoiceDate,
                   ISNULL(co.Company, ISNULL(i.CCompany,'')) AS Company,
                   '' AS FirstName,
                   '' AS LastName,
                   ISNULL(i.NettPriceTotal,0) AS PriceExGST
            FROM Invoices i
            LEFT JOIN Companies co ON i.CompanyId = co.CompanyId
            WHERE i.InvoiceDate >= @F AND i.InvoiceDate <= @T
              AND i.InvoiceStatusId = 2
            ORDER BY i.InvoiceDate",
            new() { ["F"] = dateFrom, ["T"] = dateTo });

        var sb = new StringBuilder();
        sb.AppendLine("\"Co./Last Name\",\"First Name\",\"Invoice No\",\"Date\",\"Description\",\"Amount\",\"Status\"");
        foreach (DataRow r in dt.Rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                $"\"{r["Company"]}\"",
                $"\"{r["FirstName"]}\"",
                $"\"{r["InvoiceNum"]}\"",
                $"\"{Convert.ToDateTime(r["InvoiceDate"]):dd/MM/yyyy}\"",
                "\"Invoice from Techlight\"",
                $"\"{Convert.ToDecimal(r["PriceExGST"]):N2}\"",
                "\"Open\""
            }));
        }

        int count = dt.Rows.Count;
        if (count > 0)
        {
            await _db.ExecuteAsync(@"
                UPDATE Invoices SET ExportedToMYOB = 1, ExportedDate = @Now
                WHERE InvoiceDate >= @F AND InvoiceDate <= @T AND InvoiceStatusId = 2",
                new() { ["Now"] = DateTime.Now, ["F"] = dateFrom, ["T"] = dateTo });
            await _db.InsertAsync(@"
                INSERT INTO InvoiceExportLog (ExportedBy, DateFrom, DateTo, InvoiceCount, Status)
                VALUES (@By, @F, @T, @Cnt, 'Exported')",
                new() { ["By"] = userCode, ["F"] = dateFrom, ["T"] = dateTo, ["Cnt"] = count });
        }

        return (Encoding.UTF8.GetBytes(sb.ToString()), count);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Task InsertLineAsync(int invoiceId, InvoiceLineItem l) =>
        _db.InsertAsync(@"
            INSERT INTO InvoiceContents
                (InvoiceId, Quantity, BackOrder, Ordered, Units, Days, ProductCode, Description, NettPrice, ExtNettPrice)
            VALUES
                (@InvoiceId, @Qty, 0, @Qty, @Qty, 0, @ProductCode, @Description, @NettPrice, @ExtNettPrice)",
            new()
            {
                ["InvoiceId"]   = invoiceId,
                ["Qty"]         = l.Quantity,
                ["ProductCode"] = (object?)l.ProductCode ?? DBNull.Value,
                ["Description"] = l.Description,
                ["NettPrice"]   = l.NettPrice,
                ["ExtNettPrice"]= l.ExtNettPrice,
            });

    private Task WriteAuditAsync(int invoiceId, string code, string action) =>
        _db.InsertAsync(
            "INSERT INTO InvoiceAudit (InvoiceId, Code, Action, DateEntered) VALUES (@Id, @C, @A, @D)",
            new() { ["Id"] = invoiceId, ["C"] = code, ["A"] = action, ["D"] = DateTime.Now });

    private static Dictionary<string, object?> BuildParams(Invoice inv, string userCode) => new()
    {
        ["InvoiceNum"]     = inv.InvoiceNum,
        ["InvoiceDate"]    = inv.InvoiceDate == default ? DateTime.Now : inv.InvoiceDate,
        ["Code"]           = userCode,
        ["DivisionId"]     = inv.DivisionId,
        ["Qid"]            = inv.Qid,
        ["CompanyId"]      = inv.CompanyId == 0 ? 142 : inv.CompanyId,
        ["ContactId"]      = (object?)inv.ContactId ?? DBNull.Value,
        ["InvCompany"]     = inv.InvCompany,
        ["DelCompany"]     = inv.DelCompany,
        ["InvAddress"]     = inv.InvAddress,
        ["DelAddress"]     = inv.DelAddress,
        ["CustomerPO"]     = (object?)inv.CustomerPO ?? DBNull.Value,
        ["Attention"]      = (object?)inv.Attention ?? DBNull.Value,
        ["Account"]        = (object?)inv.Account ?? DBNull.Value,
        ["Terms"]          = (object?)inv.Terms ?? DBNull.Value,
        ["CustomerNotes"]  = (object?)inv.CustomerNotes ?? DBNull.Value,
        ["InternalNotes"]  = (object?)inv.InternalNotes ?? DBNull.Value,
        ["NettPriceTotal"] = inv.NettPriceTotal,
        ["GSTTotal"]       = inv.GSTTotal,
    };

    private static Invoice MapInvoice(DataRow r) => new()
    {
        InvoiceId      = Convert.ToInt32(r["InvoiceId"]),
        InvoiceNum     = r["InvoiceNum"]?.ToString() ?? "",
        InvoiceDate    = r["InvoiceDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["InvoiceDate"]),
        Code           = r["Code"]?.ToString() ?? "",
        InvoicedBy     = r["InvoicedBy"]?.ToString() ?? "",
        InvoiceStatusId= Convert.ToInt32(r["InvoiceStatusId"]),
        StatusName     = r["StatusName"]?.ToString() ?? "",
        DivisionId     = Convert.ToInt32(r["DivisionId"]),
        DivisionName   = r["DivisionName"]?.ToString() ?? "",
        Qid            = Convert.ToInt32(r["Qid"]),
        CompanyId      = Convert.ToInt32(r["CompanyId"]),
        ContactId      = r["ContactId"] == DBNull.Value ? null : Convert.ToInt32(r["ContactId"]),
        CCompany       = r["CCompany"]?.ToString() ?? "",
        DelCompany     = r["DelCompany"]?.ToString() ?? "",
        InvAddress     = r["InvAddress"]?.ToString() ?? "",
        DelAddress     = r["DelAddress"]?.ToString() ?? "",
        CustomerPO     = r["CustomerPO"] == DBNull.Value ? null : r["CustomerPO"]?.ToString(),
        Attention      = r["Attention"] == DBNull.Value ? null : r["Attention"]?.ToString(),
        Account        = r["Account"] == DBNull.Value ? null : r["Account"]?.ToString(),
        Terms          = r["Terms"] == DBNull.Value ? null : r["Terms"]?.ToString(),
        CustomerNotes  = r["CustomerNotes"] == DBNull.Value ? null : r["CustomerNotes"]?.ToString(),
        InternalNotes  = r["InternalNotes"] == DBNull.Value ? null : r["InternalNotes"]?.ToString(),
        NettPriceTotal = Convert.ToDecimal(r["NettPriceTotal"]),
        GSTTotal       = Convert.ToDecimal(r["GSTTotal"]),
        ExportedToMYOB = r["ExportedToMYOB"] != DBNull.Value && Convert.ToBoolean(r["ExportedToMYOB"]),
        ExportedDate   = r["ExportedDate"] == DBNull.Value ? null : Convert.ToDateTime(r["ExportedDate"]),
    };
}
