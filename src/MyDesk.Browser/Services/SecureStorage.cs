using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

namespace MyDesk.Browser.Services;

/// <summary>
/// Encrypts/decrypts user credentials using Windows DPAPI (Data Protection API),
/// binding them to the current Windows user account.
/// This replaces plain-text storage of LastUserName/LastUserEmail in appsettings.json.
/// </summary>
public static class SecureStorage
{
    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyDesk",
        "Browser",
        "credentials.dat");

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MyDesk.Browser.Credentials.v1");

    /// <summary>
    /// Saves user credentials encrypted with DPAPI.
    /// Only the current Windows user can decrypt them.
    /// </summary>
    public static void SaveCredentials(string userName, string userEmail)
    {
        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(new { userName, userEmail });
            var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(CredentialsPath)!);
            File.WriteAllBytes(CredentialsPath, encrypted);
        }
        catch (CryptographicException ex)
        {
            // DPAPI fails on some CI/CD headless environments; fall back silently
            System.Diagnostics.Debug.WriteLine($"SecureStorage.Save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads previously saved credentials. Returns (null, null) if no stored data,
    /// the file is corrupt, or decryption fails (e.g. different user, different machine).
    /// </summary>
    public static (string? userName, string? userEmail) LoadCredentials()
    {
        try
        {
            if (!File.Exists(CredentialsPath))
                return (null, null);

            var encrypted = File.ReadAllBytes(CredentialsPath);
            var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            var doc = JsonDocument.Parse(decrypted);

            var name = doc.RootElement.TryGetProperty("userName", out var n) ? n.GetString() : null;
            var email = doc.RootElement.TryGetProperty("userEmail", out var e) ? e.GetString() : null;

            return (name, email);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or IOException)
        {
            // Corrupt or tampered file — delete and treat as clean slate
            TryDeleteSilently();
            return (null, null);
        }
    }

    /// <summary>
    /// Deletes the encrypted credentials file (used on logout).
    /// </summary>
    public static void ClearCredentials()
    {
        TryDeleteSilently();
    }

    private static void TryDeleteSilently()
    {
        try
        {
            if (File.Exists(CredentialsPath))
                File.Delete(CredentialsPath);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}