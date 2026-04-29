namespace MyDesk.Shared.Models;

/// <summary>
/// Static accessor for platform branding - can be used without DI injection
/// Initialized from IConfiguration at app startup
/// </summary>
public static class PlatformBranding
{
    public static string CompanyName { get; private set; } = "Techlight";
    public static string PlatformName { get; private set; } = "MyDesk";
    public static string CompanyWebsite { get; private set; } = "techlight.com.au";
    public static string SupportEmail { get; private set; } = "support@techlight.com.au";
    public static string Title => $"{CompanyName} {PlatformName}";
    public static string LoginQuote { get; private set; } = "";
    public static string LoginQuoteAuthor { get; private set; } = "";

    public static void Initialize(PlatformSettings settings)
    {
        if (settings == null) return;
        CompanyName = settings.CompanyName ?? CompanyName;
        PlatformName = settings.PlatformName ?? PlatformName;
        CompanyWebsite = settings.CompanyWebsite ?? CompanyWebsite;
        SupportEmail = settings.SupportEmail ?? SupportEmail;
        LoginQuote = settings.LoginQuote ?? "";
        LoginQuoteAuthor = settings.LoginQuoteAuthor ?? "";
    }
}