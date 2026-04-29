using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class SupplierService
{
    private readonly DatabaseService _db;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(DatabaseService db, ILogger<SupplierService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Supplier>> GetSuppliersAsync(string? search = null, int limit = 500)
    {
        var sql = $@"
            SELECT TOP {limit} c.CompanyId AS SupplierId,
                   ISNULL(c.Company, '') AS SupplierName,
                   c.Email, c.Phone, c.Address1 + ', ' + ISNULL(c.Suburb, '') + ', ' + ISNULL(c.State, '') + ' ' + ISNULL(c.PostCode, '') AS Address,
                   c.ABN, c.SupplierCode AS Category,
                   ISNULL(c.DefaultTerms, '30 Days') AS Region,
                   0 AS PortalUsername, 0 AS PortalPasswordHash, 0 AS IsPortalEnabled,
                   c.TotalSpend, c.OrderCount
            FROM Companies c
            WHERE c.IsSupplier = 1";

        var parameters = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (c.Company LIKE @Search OR c.SupplierCode LIKE @Search)";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY c.Company";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapSupplier);
    }

    public async Task<Supplier?> GetSupplierAsync(int supplierId)
    {
        var sql = @"
            SELECT c.CompanyId AS SupplierId,
                   ISNULL(c.Company, '') AS SupplierName,
                   c.Email, c.Phone, c.Address1 + ', ' + ISNULL(c.Suburb, '') + ', ' + ISNULL(c.State, '') + ' ' + ISNULL(c.PostCode, '') AS Address,
                   c.ABN, c.SupplierCode AS Category,
                   ISNULL(c.DefaultTerms, '30 Days') AS Region,
                   c.TotalSpend, c.OrderCount
            FROM Companies c
            WHERE c.CompanyId = @Id AND c.IsSupplier = 1";

        var dt = await _db.QueryAsync(sql, new() { ["Id"] = supplierId });
        return dt.Rows.Count == 0 ? null : MapSupplier(dt.Rows[0]);
    }

    public async Task<Supplier?> ValidatePortalCredentialsAsync(string username, string password)
    {
        var sql = @"
            SELECT c.CompanyId AS SupplierId,
                   ISNULL(c.Company, '') AS SupplierName,
                   c.Email, c.Phone, c.Address1 + ', ' + ISNULL(c.Suburb, '') + ', ' + ISNULL(c.State, '') + ' ' + ISNULL(c.PostCode, '') AS Address,
                   c.ABN, c.SupplierCode AS Category,
                   ISNULL(c.DefaultTerms, '30 Days') AS Region,
                   c.PortalUsername, c.PortalPasswordHash, c.IsPortalEnabled, c.PortalAccessExpires,
                   c.TotalSpend, c.OrderCount
            FROM Companies c
            WHERE c.IsSupplier = 1 AND c.IsPortalEnabled = 1
              AND (c.PortalUsername = @Username OR c.Email = @Username)";

        var dt = await _db.QueryAsync(sql, new() { ["Username"] = username });
        if (dt.Rows.Count == 0) return null;

        var hashedPassword = HashPassword(password);
        var supplier = MapSupplier(dt.Rows[0]);

        if (supplier.PortalPasswordHash != hashedPassword) return null;
        return supplier;
    }

    public async Task<Supplier?> GetCurrentPortalSupplierAsync()
    {
        return null;
    }

    public async Task UpdatePortalLoginAsync(int supplierId)
    {
        var sql = "UPDATE Companies SET PortalLastLogin = GETDATE() WHERE CompanyId = @SupplierId";
        await _db.ExecuteAsync(sql, new() { ["SupplierId"] = supplierId });
    }

    public async Task<List<SupplierScore>> GetSupplierScoresAsync()
    {
        var sql = @"
            SELECT c.CompanyId AS SupplierId,
                   ISNULL(c.Company, '') AS SupplierName,
                   ISNULL(c.Score, 50) AS Score,
                   CASE 
                       WHEN ISNULL(c.TotalSpend, 0) > 100000 THEN 'Platinum'
                       WHEN ISNULL(c.TotalSpend, 0) > 50000 THEN 'Gold'
                       WHEN ISNULL(c.TotalSpend, 0) > 10000 THEN 'Silver'
                       ELSE 'Bronze'
                   END AS Tier,
                   ISNULL(c.TotalSpend, 0) AS TotalSpend,
                   ISNULL(c.OrderCount, 0) AS OrdersThisYear,
                   ISNULL(c.OnTimePercent, 95) AS OnTimeDelivery,
                   ISNULL(c.QualityPercent, 95) AS QualityRating,
                   ISNULL(c.LastOrderDate, DATEADD(year, -1, GETDATE())) AS LastOrderDate
            FROM Companies c
            WHERE c.IsSupplier = 1
            ORDER BY c.TotalSpend DESC";

        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapSupplierScore);
    }

    private static Supplier MapSupplier(DataRow r) => new()
    {
        SupplierId = Convert.ToInt32(r["SupplierId"]),
        SupplierName = r["SupplierName"]?.ToString() ?? "",
        Email = r["Email"] == DBNull.Value ? null : r["Email"]?.ToString(),
        Phone = r["Phone"] == DBNull.Value ? null : r["Phone"]?.ToString(),
        Address = r["Address"] == DBNull.Value ? null : r["Address"]?.ToString(),
        ABN = r["ABN"] == DBNull.Value ? null : r["ABN"]?.ToString(),
        Category = r["Category"] == DBNull.Value ? null : r["Category"]?.ToString(),
        Region = r["Region"]?.ToString() ?? "NSW",
        TotalSpend = r.Table.Columns.Contains("TotalSpend") && r["TotalSpend"] != DBNull.Value ? Convert.ToDecimal(r["TotalSpend"]) : 0,
        OrderCount = r.Table.Columns.Contains("OrderCount") && r["OrderCount"] != DBNull.Value ? Convert.ToInt32(r["OrderCount"]) : 0,
    };

    private static SupplierScore MapSupplierScore(DataRow r) => new()
    {
        SupplierId = Convert.ToInt32(r["SupplierId"]),
        SupplierName = r["SupplierName"]?.ToString() ?? "",
        Score = r.Table.Columns.Contains("Score") && r["Score"] != DBNull.Value ? Convert.ToInt32(r["Score"]) : 50,
        Tier = r["Tier"]?.ToString() ?? "Bronze",
        TotalSpend = r.Table.Columns.Contains("TotalSpend") && r["TotalSpend"] != DBNull.Value ? Convert.ToDecimal(r["TotalSpend"]) : 0,
        OrdersThisYear = r.Table.Columns.Contains("OrdersThisYear") && r["OrdersThisYear"] != DBNull.Value ? Convert.ToInt32(r["OrdersThisYear"]) : 0,
        OnTimeDelivery = r.Table.Columns.Contains("OnTimeDelivery") && r["OnTimeDelivery"] != DBNull.Value ? Convert.ToDouble(r["OnTimeDelivery"]) : 95,
        QualityRating = r.Table.Columns.Contains("QualityRating") && r["QualityRating"] != DBNull.Value ? Convert.ToDouble(r["QualityRating"]) : 95,
        LastOrderDate = r.Table.Columns.Contains("LastOrderDate") && r["LastOrderDate"] != DBNull.Value ? Convert.ToDateTime(r["LastOrderDate"]) : DateTime.MinValue,
    };

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + "MyDeskSupplierPortalSalt2024");
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}