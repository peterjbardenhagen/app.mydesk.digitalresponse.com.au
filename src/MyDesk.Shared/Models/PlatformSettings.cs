namespace MyDesk.Shared.Models;

public class PlatformSettings
{
    public static string GetBrandingName() => PlatformBranding.Title;

    // Core branding
    public string PlatformName { get; set; } = "Techlight";
    public string BrandName { get; set; } = "Techlight";
    public string CompanyName { get; set; } = "Techlight";
    public string CompanyLegalName { get; set; } = "";
    public string CompanyWebsite { get; set; } = "";
    public string SupportEmail { get; set; } = "support@example.com";
    public string LogoUrl { get; set; } = "";
    public string FaviconUrl { get; set; } = "";
    public string LoginQuote { get; set; } = "";
    public string LoginQuoteAuthor { get; set; } = "";
    public string Version { get; set; } = "3.1";

    // Platform Tagline & Copyright
    public string PlatformTagline { get; set; } = "AI-Powered Business Operating System";
    public string CopyrightText { get; set; } = "";
    public string PrivacyPolicyUrl { get; set; } = "";
    public string TermsAndConditionsUrl { get; set; } = "";
    public string CookiePolicyUrl { get; set; } = "";

    // Contact Emails
    public string SalesEmail { get; set; } = "";
    public string ContactEmail { get; set; } = "support@example.com";

    // PDF styling
    public string PdfDarkBackground { get; set; } = "";
    public string PdfPrimaryColor { get; set; } = "";
    public string PdfPrimaryColorLight { get; set; } = "";
    public string PdfAccentColor { get; set; } = "";
    public string PdfAddress1 { get; set; } = "";
    public string PdfSuburb { get; set; } = "";
    public string PdfState { get; set; } = "";
    public string PdfPostCode { get; set; } = "";
    public string PdfContactPhone { get; set; } = "";
    public string PdfContactEmail { get; set; } = "";

    // Quote defaults
    public decimal GrossProfitMarginPercent { get; set; } = 30m;

    // Feature Flags
    public bool DisableAllEmails { get; set; } = false;
    public bool EnableAIAssistant { get; set; } = true;
    public bool EnableAskAI { get; set; } = true;
    public bool EnableTelegramBot { get; set; } = false;
    public bool EnableMYOBIntegration { get; set; } = false;
    public bool EnableXeroIntegration { get; set; } = false;
    public bool EnableQuickBooksIntegration { get; set; } = false;
    public bool EnableOutlookIntegration { get; set; } = false;
    public bool EnableGoogleIntegration { get; set; } = false;
    public bool EnableWeatherIntegration { get; set; } = false;
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnablePDFGeneration { get; set; } = true;
    public bool EnableTwoFactorAuth { get; set; } = false;
    public bool EnableSSO { get; set; } = false;
    public bool EnableCustomBranding { get; set; } = false;
    
    // Multi-tenancy
    public bool IsMultiTenant { get; set; } = false;
    public int MaxUsersPerTenant { get; set; } = 10;
    public int MaxStoragePerTenantMB { get; set; } = 1024;
    public int TrialDays { get; set; } = 14;
    public string SubscriptionPlan { get; set; } = "Foundation";

    // Regional Settings
    public string DefaultTimezone { get; set; } = "AUS Eastern Standard Time";
    public string DefaultCurrency { get; set; } = "AUD";
    public string DefaultDateFormat { get; set; } = "dd/MM/yyyy";
    public string DefaultCountry { get; set; } = "Australia";
    public bool UseEnglishAustralianSpelling { get; set; } = true;

    // Login Page URLs
    public string LoginLogoUrl { get; set; } = "";
    public string LoginMarkUrl { get; set; } = "";
    public string LoginQuoteLabel { get; set; } = "";
    public string LoginHeading { get; set; } = "Sign in to MyDesk";
    public string LoginSubheading { get; set; } = "";
    public string LoginPrimaryColor { get; set; } = "#00c8c8";
    public string LoginAccentColor { get; set; } = "#2196F3";
    public string LoginBackgroundColor { get; set; } = "";

    // Integration Settings
    public IntegrationSettings MYOB { get; set; } = new();
    public IntegrationSettings Xero { get; set; } = new();
    public IntegrationSettings QuickBooks { get; set; } = new();
    public IntegrationSettings Outlook { get; set; } = new();
    public IntegrationSettings MyOutlook { get; set; } = new();
    public Dictionary<string, IntegrationSettings> MyOutlookUserConnections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public IntegrationSettings Google { get; set; } = new();
    public IntegrationSettings Weather { get; set; } = new();

    // Outlook AI settings (applied per-mailbox)
    public bool OutlookAutoCategorise { get; set; } = true;
    public bool OutlookAutoDraftReplies { get; set; } = false;
    public bool OutlookMoveToCompanyFolder { get; set; } = true;

    // Misc defaults
    public string CompanyAddress { get; set; } = "";

    // Brand assets and links (loaded from JSON settings file)
    public List<BrandAssetFile> BrandAssets { get; set; } = new();
    public List<ImportantLink> ImportantLinks { get; set; } = new();
}

public class IntegrationSettings
{
    public bool Enabled { get; set; } = false;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public DateTime? LastSync { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string? RedirectUri { get; set; }
    public string? AuthEndpoint { get; set; }
    public bool SyncInvoices { get; set; }
    public bool SyncQuotes { get; set; }
    public bool SyncContacts { get; set; }
    public bool AutoExportInvoices { get; set; }
    public string? Status { get; set; }
    public bool IsConnected { get; set; }
    public IntegrationStatus IntegrationStatus { get; set; } = new();
    public bool SyncCalendar { get; set; }
    public bool SyncEmail { get; set; }
    public bool SyncDrive { get; set; }
    public bool SyncGmail { get; set; }
}
