using MyDesk.Browser.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using Xunit;

namespace MyDesk.Browser.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _settingsDir;

    public SettingsViewModelTests()
    {
        _settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyDesk", "Browser");
        // Clear any saved settings before each test
        if (Directory.Exists(_settingsDir))
            Directory.Delete(_settingsDir, true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDir))
            Directory.Delete(_settingsDir, true);
    }

    [Fact]
    public void Constructor_LoadsDefaultSettings()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);

        Assert.Equal("https://app.mydesk.digitalresponse.com.au", vm.DefaultUrl);
        Assert.Equal("MyDesk Browser", vm.WindowTitle);
        Assert.Equal(1400, vm.WindowWidth);
        Assert.Equal(900, vm.WindowHeight);
        Assert.False(vm.StartMaximized);
        Assert.True(vm.RememberWindowState);
    }

    [Fact]
    public void ResetToDefaults_RestoresAllDefaults()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);
        vm.DefaultUrl = "https://custom.example.com";
        vm.WindowTitle = "Custom Title";
        vm.WindowWidth = 1024;
        vm.EnableDevTools = true;

        vm.ResetToDefaultsCommand.Execute(null);

        Assert.Equal("https://app.mydesk.digitalresponse.com.au", vm.DefaultUrl);
        Assert.Equal("MyDesk Browser", vm.WindowTitle);
        Assert.Equal(1400, vm.WindowWidth);
        Assert.False(vm.EnableDevTools);
    }

    [Fact]
    public void ResetToDefaults_ShowsStatusMessage()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);
        vm.ResetToDefaultsCommand.Execute(null);

        Assert.Contains("Reset to defaults", vm.StatusMessage);
    }

    [Fact]
    public void Properties_RoundTripCorrectly()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);

        vm.DefaultUrl = "https://corp.example.com";
        vm.WindowTitle = "Corp Portal";
        vm.WindowWidth = 1920;
        vm.WindowHeight = 1080;
        vm.StartMaximized = true;
        vm.ShowToolbar = false;
        vm.EnableDevTools = true;
        vm.AllowExternalLinks = false;
        vm.AutoGrantPermissions = false;
        vm.HardwareAcceleration = false;

        Assert.Equal("https://corp.example.com", vm.DefaultUrl);
        Assert.Equal("Corp Portal", vm.WindowTitle);
        Assert.Equal(1920, vm.WindowWidth);
        Assert.Equal(1080, vm.WindowHeight);
        Assert.True(vm.StartMaximized);
        Assert.False(vm.ShowToolbar);
        Assert.True(vm.EnableDevTools);
        Assert.False(vm.AllowExternalLinks);
        Assert.False(vm.AutoGrantPermissions);
        Assert.False(vm.HardwareAcceleration);
    }

    [Fact]
    public void Version_ReturnsNonEmptyString()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);
        Assert.False(string.IsNullOrWhiteSpace(vm.Version));
    }

    [Fact]
    public void ClientPreset_Techlight_OverridesCorrectly()
    {
        var vm = new SettingsViewModel(NullLogger<SettingsViewModel>.Instance);

        // Simulate loading a Techlight client preset
        vm.DefaultUrl = "https://app.techlight.com.au";
        vm.WindowTitle = "Techlight - MyDesk";

        Assert.Equal("https://app.techlight.com.au", vm.DefaultUrl);
        Assert.Equal("Techlight - MyDesk", vm.WindowTitle);
    }
}
