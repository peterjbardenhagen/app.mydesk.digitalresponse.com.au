using System.Data;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class FavouritesService
{
    private readonly DatabaseService _db;
    private readonly ILogger<FavouritesService> _logger;

    public FavouritesService(DatabaseService db, ILogger<FavouritesService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<FavouriteItem>> GetUserFavouritesAsync(string userCode)
    {
        var dt = await _db.QueryAsync(@"
            SELECT FavouriteId, EntityType, EntityId, EntityName, Notes, CreatedAt
            FROM Favourites
            WHERE UserCode = @Code
            ORDER BY CreatedAt DESC",
            new() { ["Code"] = userCode });

        return dt.Rows.Cast<DataRow>().Select(r => new FavouriteItem
        {
            FavouriteId = Convert.ToInt32(r["FavouriteId"]),
            EntityType = r["EntityType"].ToString()!,
            EntityId = Convert.ToInt32(r["EntityId"]),
            EntityName = r["EntityName"]?.ToString() ?? "",
            Notes = r["Notes"]?.ToString() ?? "",
            CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
        }).ToList();
    }

    public async Task<bool> IsFavouriteAsync(string userCode, string entityType, int entityId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT COUNT(*) AS Cnt FROM Favourites
            WHERE UserCode = @Code AND EntityType = @Type AND EntityId = @Id",
            new() { ["Code"] = userCode, ["Type"] = entityType, ["Id"] = entityId });

        return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Cnt"]) > 0;
    }

    public async Task AddFavouriteAsync(string userCode, string entityType, int entityId, string? entityName = null, string? notes = null)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO Favourites (UserCode, EntityType, EntityId, EntityName, Notes)
            VALUES (@Code, @Type, @Id, @Name, @Notes)",
            new() { ["Code"] = userCode, ["Type"] = entityType, ["Id"] = entityId,
                    ["Name"] = entityName ?? "", ["Notes"] = notes ?? "" });

        _logger.LogInformation("Favourite added: {UserCode} -> {Type} {Id}", userCode, entityType, entityId);
    }

    public async Task RemoveFavouriteAsync(string userCode, string entityType, int entityId)
    {
        await _db.ExecuteAsync(@"
            DELETE FROM Favourites
            WHERE UserCode = @Code AND EntityType = @Type AND EntityId = @Id",
            new() { ["Code"] = userCode, ["Type"] = entityType, ["Id"] = entityId });

        _logger.LogInformation("Favourite removed: {UserCode} -> {Type} {Id}", userCode, entityType, entityId);
    }

    public async Task RemoveFavouriteByIdAsync(int favourtieId, string userCode)
    {
        await _db.ExecuteAsync(@"
            DELETE FROM Favourites
            WHERE FavouriteId = @Id AND UserCode = @Code",
            new() { ["Id"] = favourtieId, ["Code"] = userCode });
    }
}

public class FavouriteItem
{
    public int FavouriteId { get; set; }
    public string EntityType { get; set; } = ""; // Quote, Invoice, PurchaseOrder, Contact, Company, Product
    public int EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public string GetRoute() => EntityType switch
    {
        "Quote" => $"/quotes/{EntityId}",
        "Invoice" => $"/invoices/{EntityId}",
        "PurchaseOrder" => $"/purchase-orders/{EntityId}",
        "Contact" => $"/contacts/{EntityId}",
        "Company" => $"/companies/{EntityId}",
        "Product" => $"/products/{EntityId}",
        _ => "/"
    };

    public string GetIcon() => EntityType switch
    {
        "Quote" => "request_quote",
        "Invoice" => "receipt_long",
        "PurchaseOrder" => "shopping_cart",
        "Contact" => "person",
        "Company" => "business",
        "Product" => "inventory",
        _ => "folder"
    };
}
