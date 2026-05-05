using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private readonly ICurrentTenantAccessor? _currentTenantAccessor;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger, ICurrentTenantAccessor? currentTenantAccessor = null)
    {
        _connectionString = configuration.GetConnectionString("TechlightDb")
            ?? "Server=localhost;Database=Techlight;Trusted_Connection=True;TrustServerCertificate=True;";
        _logger = logger;
        _currentTenantAccessor = currentTenantAccessor;
    }

    public async Task<SqlConnection> GetConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await ApplyTenantSessionContextAsync(conn);
        return conn;
    }

    private async Task ApplyTenantSessionContextAsync(SqlConnection conn)
    {
        if (_currentTenantAccessor == null)
        {
            return;
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
EXEC sys.sp_set_session_context @key=N'BypassTenantIsolation', @value=@Bypass;
EXEC sys.sp_set_session_context @key=N'TenantId', @value=@TenantId;";
        cmd.Parameters.AddWithValue("@Bypass", _currentTenantAccessor.BypassTenantIsolation ? 1 : 0);
        cmd.Parameters.AddWithValue("@TenantId", (object?)_currentTenantAccessor.TenantId?.ToString() ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private void ApplyTenantSessionContext(SqlConnection conn)
    {
        if (_currentTenantAccessor == null)
        {
            return;
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
EXEC sys.sp_set_session_context @key=N'BypassTenantIsolation', @value=@Bypass;
EXEC sys.sp_set_session_context @key=N'TenantId', @value=@TenantId;";
        cmd.Parameters.AddWithValue("@Bypass", _currentTenantAccessor.BypassTenantIsolation ? 1 : 0);
        cmd.Parameters.AddWithValue("@TenantId", (object?)_currentTenantAccessor.TenantId?.ToString() ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // Dapper-based generic methods (for model classes)
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            return await conn.QueryAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Query Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            return await conn.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Query Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            return await conn.ExecuteScalarAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Scalar Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            return await conn.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Execute Error: {Sql}", sql);
            throw;
        }
    }

    // ADO.NET-based methods (for Dictionary parameters)
    public async Task<DataTable> QueryAsync(string sql, Dictionary<string, object?>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, parameters);

            var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Query Error: {Sql}", sql);
            throw;
        }
    }

    public DataTable Query(string sql, Dictionary<string, object?>? parameters = null)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            ApplyTenantSessionContext(conn);
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, parameters);

            var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Sync Query Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<T?> ScalarAsync<T>(string sql, Dictionary<string, object?>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, parameters);

            var result = await cmd.ExecuteScalarAsync();
            if (result is null or DBNull) return default;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var converted = Convert.ChangeType(result, targetType);
            // Handle nullable return types properly
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
                return (T?)converted;
            return (T)converted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Scalar Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteObjAsync(string sql, object? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            return await conn.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Execute Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object?>? parameters = null)
    {
        try
        {
            using var conn = await GetConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            AddParameters(cmd, parameters);

            return await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Execute Error: {Sql}", sql);
            throw;
        }
    }

    public async Task<int> InsertAsync(string sql, Dictionary<string, object?>? parameters = null)
    {
        sql = sql.TrimEnd(';') + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await ScalarAsync<int>(sql, parameters);
    }

    public List<Dictionary<string, object?>> ToList(DataTable dt)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in dt.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            result.Add(dict);
        }
        return result;
    }

    private static void AddParameters(SqlCommand cmd, Dictionary<string, object?>? parameters)
    {
        if (parameters == null) return;
        foreach (var (key, value) in parameters)
        {
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
        }
    }
}
