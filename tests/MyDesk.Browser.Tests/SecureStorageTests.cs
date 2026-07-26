using MyDesk.Browser.Services;
using Xunit;

namespace MyDesk.Browser.Tests;

public class SecureStorageTests
{
    [Fact]
    public void SaveAndLoadCredentials_RoundTrips()
    {
        SecureStorage.ClearCredentials();

        SecureStorage.SaveCredentials("Alice Smith", "alice@techlight.com.au");
        var (name, email) = SecureStorage.LoadCredentials();

        Assert.Equal("Alice Smith", name);
        Assert.Equal("alice@techlight.com.au", email);
    }

    [Fact]
    public void LoadCredentials_NoFile_ReturnsNulls()
    {
        SecureStorage.ClearCredentials();
        var (name, email) = SecureStorage.LoadCredentials();

        Assert.Null(name);
        Assert.Null(email);
    }

    [Fact]
    public void ClearCredentials_RemovesData()
    {
        SecureStorage.SaveCredentials("Bob", "bob@example.com");
        SecureStorage.ClearCredentials();

        var (name, email) = SecureStorage.LoadCredentials();
        Assert.Null(name);
        Assert.Null(email);
    }

    [Fact]
    public void SaveCredentials_OverwritesPreviousData()
    {
        SecureStorage.ClearCredentials();

        SecureStorage.SaveCredentials("Old Name", "old@email.com");
        SecureStorage.SaveCredentials("New Name", "new@email.com");

        var (name, email) = SecureStorage.LoadCredentials();
        Assert.Equal("New Name", name);
        Assert.Equal("new@email.com", email);
    }

    [Fact]
    public void TechlightCredentials_SaveAndLoad()
    {
        SecureStorage.ClearCredentials();

        SecureStorage.SaveCredentials("Techlight Admin", "admin@techlight.com.au");
        var (name, email) = SecureStorage.LoadCredentials();

        Assert.Equal("Techlight Admin", name);
        Assert.Equal("admin@techlight.com.au", email);
    }
}
