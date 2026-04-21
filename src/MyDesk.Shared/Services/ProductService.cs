using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ProductService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(DatabaseService db, ILogger<ProductService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<Product>> GetAllAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT ProductId, ISNULL(ProductName,'') AS ProductName, ISNULL(ProductDesc,'') AS Description,
                   UnitCost, ISNULL(MinNettPrice,0) AS UnitPrice
            FROM Products ORDER BY ProductName");
        return dt.Map(r => new Product
        {
            ProductId   = (int)r["ProductId"],
            ProductName = r["ProductName"]?.ToString() ?? "",
            Description = r["Description"]?.ToString(),
            UnitCost    = r["UnitCost"]  == DBNull.Value ? null : Convert.ToDecimal(r["UnitCost"]),
            UnitPrice   = r["UnitPrice"] == DBNull.Value ? null : Convert.ToDecimal(r["UnitPrice"]),
        }).ToList();
    }

    public async Task<Product?> GetAsync(int id)
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP 1 ProductId, ISNULL(ProductName,'') AS ProductName, ISNULL(ProductDesc,'') AS Description, UnitCost, ISNULL(MinNettPrice,0) AS UnitPrice FROM Products WHERE ProductId = @id",
            new() { ["id"] = id });
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new Product
        {
            ProductId   = (int)r["ProductId"],
            ProductName = r["ProductName"]?.ToString() ?? "",
            Description = r["Description"]?.ToString(),
            UnitCost    = r["UnitCost"]  == DBNull.Value ? null : Convert.ToDecimal(r["UnitCost"]),
            UnitPrice   = r["UnitPrice"] == DBNull.Value ? null : Convert.ToDecimal(r["UnitPrice"]),
        };
    }

    public async Task<int> SaveAsync(Product p)
    {
        if (p.ProductId == 0)
        {
            return await _db.InsertAsync(
                @"INSERT INTO Products (ProductName, ProductDesc, UnitCost, MinNettPrice)
                  VALUES (@Name, @Desc, @Cost, @Price)",
                new()
                {
                    ["Name"] = p.ProductName,
                    ["Desc"] = (object?)p.Description ?? DBNull.Value,
                    ["Cost"] = (object?)p.UnitCost    ?? DBNull.Value,
                    ["Price"] = (object?)p.UnitPrice  ?? DBNull.Value,
                });
        }

        await _db.ExecuteAsync(
            @"UPDATE Products SET ProductName=@Name, ProductDesc=@Desc, UnitCost=@Cost, MinNettPrice=@Price
              WHERE ProductId = @Id",
            new()
            {
                ["Id"] = p.ProductId,
                ["Name"] = p.ProductName,
                ["Desc"] = (object?)p.Description ?? DBNull.Value,
                ["Cost"] = (object?)p.UnitCost    ?? DBNull.Value,
                ["Price"] = (object?)p.UnitPrice  ?? DBNull.Value,
            });
        return p.ProductId;
    }

    public async Task DeleteAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Products WHERE ProductId = @id", new() { ["id"] = id });
}
