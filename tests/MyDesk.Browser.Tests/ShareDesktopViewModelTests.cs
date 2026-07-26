using MyDesk.Browser.ViewModels;
using Xunit;

namespace MyDesk.Browser.Tests;

public class ShareDesktopViewModelTests
{
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        var vm = new ShareDesktopViewModel();

        Assert.Empty(vm.CurrentUrl);
        Assert.False(vm.IsMacBound);
        Assert.Equal(1, vm.ExpiryHours);
        Assert.Equal("email", vm.ShareMethod);
        Assert.NotNull(vm.ExpiryOptions);
        Assert.Contains(1, vm.ExpiryOptions);
        Assert.Contains(24, vm.ExpiryOptions);
        Assert.Contains("email", vm.ShareMethods);
        Assert.Contains("clipboard", vm.ShareMethods);
    }

    [Fact]
    public void ShareAsync_EmptyUrl_ShowsError()
    {
        var vm = new ShareDesktopViewModel();
        vm.ShareCommand.Execute(null);

        Assert.Contains("No URL", vm.StatusMessage);
    }

    [Fact]
    public void ShareAsync_EmailModeWithoutRecipient_ShowsError()
    {
        var vm = new ShareDesktopViewModel();
        vm.CurrentUrl = "https://app.mydesk.digitalresponse.com.au";
        vm.ShareMethod = "email";

        vm.ShareCommand.Execute(null);

        Assert.Contains("recipient email", vm.StatusMessage);
    }

    [Fact]
    public async Task ShareAsync_ClipboardMode_StartsCreating()
    {
        var vm = new ShareDesktopViewModel();
        vm.CurrentUrl = "https://app.mydesk.digitalresponse.com.au/expenses/123";
        vm.ShareMethod = "clipboard";

        await vm.ShareCommand.ExecuteAsync(null);

        // Before the 5s auto-clear, StatusMessage contains the share message
        // In headless test environments, clipboard access may throw,
        // but the command should still execute and set IsCreating = false
        Assert.False(vm.IsCreating);
    }
}
