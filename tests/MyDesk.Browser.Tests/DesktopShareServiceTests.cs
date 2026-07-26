using MyDesk.Browser.Models;
using MyDesk.Browser.Services;
using System.IO;
using System.Text;
using Xunit;

namespace MyDesk.Browser.Tests;

public class DesktopShareServiceTests : IDisposable
{
    private readonly DesktopShareService _service = new();

    public void Dispose()
    {
        // Clean up any files created during testing
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyDesk", "Browser", "DesktopShares");
        if (Directory.Exists(appData))
            Directory.Delete(appData, true);
    }

    [Fact]
    public void CreateShare_GeneratesEncryptedToken()
    {
        var share = _service.CreateShare(
            "https://app.mydesk.digitalresponse.com.au/dashboard",
            "recipient@example.com",
            macBound: false,
            expiryHours: 24);

        Assert.NotNull(share);
        Assert.Equal("Active", share.Status);
        Assert.Equal("recipient@example.com", share.RecipientEmail);
        Assert.False(string.IsNullOrWhiteSpace(share.Token));
        Assert.True(share.ExpiresAt > DateTime.Now);
        Assert.True(share.ExpiresAt <= DateTime.Now.AddHours(24));
    }

    [Fact]
    public void CreateShare_WithMacBinding_SetsMacBound()
    {
        var share = _service.CreateShare(
            "https://app.mydesk.digitalresponse.com.au",
            "user@techlight.com.au",
            macBound: true,
            expiryHours: 1);

        Assert.True(share.IsMacBound);
    }

    [Fact]
    public void CreateShare_ExpiryHonorsSpecifiedHours()
    {
        var share = _service.CreateShare("https://app.mydesk.digitalresponse.com.au",
            "test@example.com", expiryHours: 2);

        var expectedMax = DateTime.Now.AddHours(2);
        Assert.True(share.ExpiresAt <= expectedMax,
            $"ExpiresAt {share.ExpiresAt} should be <= {expectedMax}");
    }

    [Fact]
    public void RevokeShare_MarksShareAsRevoked()
    {
        var share = _service.CreateShare(
            "https://app.mydesk.digitalresponse.com.au",
            "test@example.com");

        _service.RevokeShare(share.Id);

        Assert.Equal("Revoked", share.Status);
    }

    [Fact]
    public void CleanupExpired_ExpiredSharesBecomeExpired()
    {
        var share = _service.CreateShare(
            "https://app.mydesk.digitalresponse.com.au",
            "test@example.com",
            expiryHours: -1); // Already expired

        _service.CleanupExpired();

        Assert.Equal("Expired", share.Status);
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokens()
    {
        var token1 = DesktopShare.GenerateToken();
        var token2 = DesktopShare.GenerateToken();

        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
        // Token is base64 encoded random bytes, will be <= 32 chars
        Assert.True(token1.Length > 0, "Token should not be empty");
    }

    [Fact]
    public void BuildShareUrl_ProducesValidUrl()
    {
        var rawToken = "test-token-12345";
        var baseUrl = "https://app.mydesk.digitalresponse.com.au";
        var url = DesktopShare.BuildShareUrl(rawToken, baseUrl);

        Assert.Contains("shared-desktop", url);
        Assert.Contains(rawToken, url);
        Assert.StartsWith(baseUrl, url);
    }

    [Fact]
    public void TokenLength_ReturnsReasonableSize()
    {
        var token = DesktopShare.GenerateToken(32);
        // Base64 encoding of 32 bytes ~ 43 chars, but truncated to 32
        Assert.True(token.Length >= 24, $"Token length {token.Length} should be >= 24");
    }

    [Fact]
    public void TechlightShare_CreatesCorrectly()
    {
        var share = _service.CreateShare(
            "https://app.techlight.com.au/projects",
            "manager@techlight.com.au",
            macBound: true,
            expiryHours: 48);

        Assert.Equal("Active", share.Status);
        Assert.Equal("manager@techlight.com.au", share.RecipientEmail);
        Assert.StartsWith("https://app.techlight.com.au", share.SharedUrl);
        Assert.True(share.IsMacBound);
    }
}
