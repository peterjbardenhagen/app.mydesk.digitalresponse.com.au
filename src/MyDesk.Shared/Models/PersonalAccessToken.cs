namespace MyDesk.Shared.Models;

/// <summary>
/// A long-lived personal access token issued to a specific user+tenant pair
/// for use by external AI agents (MCP, ChatGPT Actions, Copilot, Gemini, etc.).
///
/// The raw token is shown only once at creation time.
/// Only the SHA-256 hex digest (<see cref="TokenHash"/>) is stored in the database.
///
/// Token format:  mdk_{base64url(32 random bytes)}
/// </summary>
public class PersonalAccessToken
{
    public Guid      TokenId     { get; set; } = Guid.NewGuid();
    public int       UserId      { get; set; }
    public Guid      TenantId    { get; set; }
    public string    TokenName   { get; set; } = "";
    public string    TokenHash   { get; set; } = "";

    /// <summary>Space-separated scope list, e.g. "chat tools accounting".</summary>
    public string    Scopes      { get; set; } = "chat tools";

    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt   { get; set; }
    public DateTime? LastUsedAt  { get; set; }
    public bool      IsRevoked   { get; set; }

    // ── Populated by service layer (not stored in the token row) ──────────
    public string? UserName   { get; set; }
    public string? UserCode   { get; set; }
    public string? TenantName { get; set; }
    public string? UserRole   { get; set; }
}
