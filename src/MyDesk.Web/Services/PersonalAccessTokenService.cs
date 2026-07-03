using System.Data;
using System.Security.Cryptography;
using System.Text;
using MyDesk.Shared.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

/// <summary>
/// Generates, validates, and revokes Personal Access Tokens used by external AI agents.
///
/// Security properties:
/// - Raw token is never stored; only SHA-256 hex digest.
/// - Index on TokenHash (WHERE IsRevoked = 0) keeps validation O(1).
/// - <see cref="ValidateAsync"/> updates LastUsedAt fire-and-forget so latency is unaffected.
/// </summary>
public class PersonalAccessTokenService
{
    private readonly DatabaseService _db;

    public PersonalAccessTokenService(DatabaseService db) => _db = db;

    // ── Generate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new PAT. Returns the raw token (show once, never store) and the
    /// persisted record (hash only).
    /// </summary>
    public async Task<(string RawToken, PersonalAccessToken Record)> GenerateAsync(
        int userId, Guid tenantId, string tokenName,
        string scopes  = "chat tools",
        DateTime? expiresAt = null)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = "mdk_" + Convert.ToBase64String(randomBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var hash = HashToken(rawToken);
        var record = new PersonalAccessToken
        {
            UserId    = userId,
            TenantId  = tenantId,
            TokenName = tokenName.Trim(),
            TokenHash = hash,
            Scopes    = scopes,
            ExpiresAt = expiresAt,
        };

        await _db.ExecuteNonQueryAsync(@"
            INSERT INTO PersonalAccessTokens
                (TokenId, UserId, TenantId, TokenName, TokenHash, Scopes, CreatedAt, ExpiresAt)
            VALUES
                (@TokenId, @UserId, @TenantId, @TokenName, @TokenHash, @Scopes, GETUTCDATE(), @ExpiresAt)",
            new()
            {
                ["TokenId"]   = record.TokenId,
                ["UserId"]    = userId,
                ["TenantId"]  = tenantId,
                ["TokenName"] = record.TokenName,
                ["TokenHash"] = hash,
                ["Scopes"]    = scopes,
                ["ExpiresAt"] = (object?)expiresAt ?? DBNull.Value,
            });

        return (rawToken, record);
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a raw token string. Returns the hydrated record (user + tenant metadata)
    /// or null if the token is invalid, revoked, or expired.
    /// </summary>
    public async Task<PersonalAccessToken?> ValidateAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || !rawToken.StartsWith("mdk_"))
            return null;

        var hash = HashToken(rawToken);
        var dt = await _db.QueryAsync(@"
            SELECT
                p.TokenId, p.UserId, p.TenantId, p.TokenName, p.Scopes,
                p.CreatedAt, p.ExpiresAt,
                u.Code  AS UserCode,
                u.Name  AS UserName,
                u.Role  AS UserRole,
                t.Name  AS TenantName
            FROM  PersonalAccessTokens  p
            INNER JOIN Users   u ON u.UserId   = p.UserId
            INNER JOIN Tenants t ON t.TenantId = p.TenantId
            WHERE p.TokenHash = @Hash
              AND p.IsRevoked  = 0
              AND (p.ExpiresAt IS NULL OR p.ExpiresAt > GETUTCDATE())",
            new() { ["Hash"] = hash });

        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        var token = new PersonalAccessToken
        {
            TokenId    = Guid.Parse(row["TokenId"]!.ToString()!),
            UserId     = Convert.ToInt32(row["UserId"]),
            TenantId   = Guid.Parse(row["TenantId"]!.ToString()!),
            TokenName  = row["TokenName"]?.ToString() ?? "",
            TokenHash  = hash,
            Scopes     = row["Scopes"]?.ToString() ?? "",
            ExpiresAt  = row["ExpiresAt"]  is DBNull ? null : Convert.ToDateTime(row["ExpiresAt"]),
            UserCode   = row["UserCode"]?.ToString(),
            UserName   = row["UserName"]?.ToString(),
            UserRole   = row["UserRole"]?.ToString(),
            TenantName = row["TenantName"]?.ToString(),
        };

        // Update last-used timestamp asynchronously — don't await to avoid latency hit
        _ = _db.ExecuteNonQueryAsync(
            "UPDATE PersonalAccessTokens SET LastUsedAt = GETUTCDATE() WHERE TokenId = @Id",
            new() { ["Id"] = token.TokenId });

        return token;
    }

    // ── List ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active (non-revoked) tokens for a user+tenant pair.
    /// TokenHash is NOT returned — only metadata.
    /// </summary>
    public async Task<List<PersonalAccessToken>> ListAsync(int userId, Guid tenantId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT TokenId, TokenName, Scopes, CreatedAt, ExpiresAt, LastUsedAt
            FROM   PersonalAccessTokens
            WHERE  UserId   = @UserId
              AND  TenantId = @TenantId
              AND  IsRevoked = 0
            ORDER BY CreatedAt DESC",
            new() { ["UserId"] = userId, ["TenantId"] = tenantId });

        return dt.Rows.Cast<DataRow>().Select(r => new PersonalAccessToken
        {
            TokenId    = Guid.Parse(r["TokenId"]!.ToString()!),
            TokenName  = r["TokenName"]?.ToString() ?? "",
            Scopes     = r["Scopes"]?.ToString() ?? "",
            CreatedAt  = Convert.ToDateTime(r["CreatedAt"]),
            ExpiresAt  = r["ExpiresAt"]  is DBNull ? null : Convert.ToDateTime(r["ExpiresAt"]),
            LastUsedAt = r["LastUsedAt"] is DBNull ? null : Convert.ToDateTime(r["LastUsedAt"]),
        }).ToList();
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a token. Only succeeds if the token belongs to the given user+tenant
    /// (prevents cross-user revocation).
    /// </summary>
    public async Task<bool> RevokeAsync(Guid tokenId, int userId, Guid tenantId)
    {
        var rows = await _db.ExecuteNonQueryAsync(@"
            UPDATE PersonalAccessTokens
            SET    IsRevoked = 1
            WHERE  TokenId  = @Id
              AND  UserId   = @UserId
              AND  TenantId = @TenantId",
            new() { ["Id"] = tokenId, ["UserId"] = userId, ["TenantId"] = tenantId });

        return rows > 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
               .ToLowerInvariant();
}
