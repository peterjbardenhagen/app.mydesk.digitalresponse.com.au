using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace MyDesk.Browser.Models
{
    /// <summary>
    /// Represents a shared desktop session with encrypted token and optional MAC binding.
    /// </summary>
    public class DesktopShare
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddHours(1);

        [JsonPropertyName("sharedUrl")]
        public string SharedUrl { get; set; } = string.Empty;

        [JsonPropertyName("recipientEmail")]
        public string RecipientEmail { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("macAddress")]
        public string MacAddress { get; set; } = string.Empty;

        [JsonPropertyName("isMacBound")]
        public bool IsMacBound { get; set; } = false;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [JsonPropertyName("viewCount")]
        public int ViewCount { get; set; } = 0;

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Active" => "🔗",
            "Expired" => "⏰",
            "Revoked" => "🔒",
            _ => "📋"
        };

        [JsonIgnore]
        public string ShortDate => CreatedAt.ToString("MMM dd, yyyy HH:mm");

        [JsonIgnore]
        public string ExpiryDate => ExpiresAt.ToString("MMM dd, yyyy HH:mm");

        [JsonIgnore]
        public string TokenDisplay => Token.Length > 12 ? $"{Token[..6]}...{Token[^4..]}" : Token;

        [JsonIgnore]
        public bool IsExpired => DateTime.Now > ExpiresAt;

        /// <summary>
        /// Generates a cryptographically secure share token.
        /// </summary>
        public static string GenerateToken(int length = 24)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                [..Math.Min(length, 32)];
        }

        /// <summary>
        /// Creates a share URL from the token and base URL.
        /// </summary>
        public static string BuildShareUrl(string token, string baseUrl)
        {
            var encoded = Uri.EscapeDataString(token);
            return $"{baseUrl}/shared-desktop?token={encoded}";
        }

        /// <summary>
        /// Validates the token expiry and optional MAC binding.
        /// </summary>
        public bool Validate(string clientMac = "")
        {
            if (IsExpired) return false;
            if (IsMacBound && !string.IsNullOrEmpty(MacAddress))
            {
                return string.Equals(MacAddress, clientMac, StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }
    }

    /// <summary>
    /// Helper for encrypting share tokens for secure transport.
    /// </summary>
    public static class ShareTokenHelper
    {
        /// <summary>
        /// Creates a simple encrypted payload (machine-scoped DPAPI-like protection).
        /// Note: For production, this should use the machine key store or Azure Key Vault.
        /// </summary>
        public static string EncryptToken(string token)
        {
            var data = Encoding.UTF8.GetBytes(token);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a previously encrypted token.
        /// </summary>
        public static string DecryptToken(string encryptedToken)
        {
            var data = Convert.FromBase64String(encryptedToken);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
