using System.Data;
using Microsoft.Data.SqlClient;

namespace Techlight.MyDesk.MCP.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetConnectionString()
    {
        // Try to get from configuration first, otherwise use default
        var connString = _configuration.GetConnectionString("MyDeskDatabase");
        
        if (string.IsNullOrEmpty(connString))
        {
            // Default connection - you may need to update this
            connString = "Server=localhost;Database=MyDesk;Trusted_Connection=True;TrustServerCertificate=True;";
        }
        
        return connString;
    }

    public async Task<SqlConnection> GetConnectionAsync()
    {
        var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();
        return conn;
    }

    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }
            
            var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            
            return dt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }
            
            var result = await cmd.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return default;
                
            return (T?)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }
            
            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing non-query: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteInsertAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        // Append SELECT SCOPE_IDENTITY() to get the inserted ID
        sql = sql.TrimEnd(';') + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
        
        var result = await ExecuteScalarAsync<int>(sql, parameters);
        return result;
    }

    // Helper method to convert DataTable to List of Dictionaries
    public List<Dictionary<string, object>> DataTableToList(DataTable dt)
    {
        var result = new List<Dictionary<string, object>>();
        
        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
            {
                var value = row[col];
                dict[col.ColumnName] = value == DBNull.Value ? null! : value;
            }
            result.Add(dict);
        }
        
        return result;
    }
}
