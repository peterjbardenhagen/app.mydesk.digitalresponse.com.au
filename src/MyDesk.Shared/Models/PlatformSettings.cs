namespace MyDesk.Shared.Models;

/// <summary>
/// Configuration for a single Telegram bot environment
/// </summary>
public class TelegramBotConfig
{
    public string BotToken { get; set; } = "";
    public string BotUsername { get; set; } = "";
    public string[] AllowedUsers { get; set; } = Array.Empty<string>();
    public long[] AllowedChatIds { get; set; } = Array.Empty<long>();
    public string WebhookUrl { get; set; } = "";
    public string Environment { get; set; } = "prod"; // prod, dev, staging, etc.
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> CustomCommands { get; set; } = new();
}

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

    // AI Provider (BYOAI — each tenant chooses their own AI backend)
    public AiProviderConfig AiProvider { get; set; } = new();

    // Legal module flags (CCL — Carter Capner Law and similar firms)
    public bool EnableLegalModules { get; set; } = false;
    public string RadixApiUrl { get; set; } = "";
    public string RadixApiKey { get; set; } = "";
    public string PracticeEvolveConnectionString { get; set; } = "";

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

    // Client Notifications (SMS + Email)
    public NotificationSettings Notifications { get; set; } = new();

    // Telegram Bot Configuration (simple prod/dev)
    public TelegramSettings Telegram { get; set; } = new();

    // Telegram Bot Configuration (flexible multi-environment)
    /// <summary>
    /// Dictionary of bot configurations by environment name (e.g., "prod", "dev", "staging")
    /// This provides more flexibility than the simple ProdBotToken/DevBotToken approach.
    /// </summary>
    public Dictionary<string, TelegramBotConfig> TelegramBots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class TelegramSettings
{
    /// <summary>Production bot token from @BotFather</summary>
    public string? ProdBotToken { get; set; }

    /// <summary>Development bot token from @BotFather</summary>
    public string? DevBotToken { get; set; }

    /// <summary>Telegram usernames (without @) allowed to use the bot</summary>
    public string[] AllowedUsers { get; set; } = Array.Empty<string>();

    /// <summary>Telegram chat IDs allowed to use the bot (optional, more secure than usernames)</summary>
    public long[] AllowedChatIds { get; set; } = Array.Empty<long>();

    /// <summary>Default environment: "prod" or "dev"</summary>
    public string DefaultEnvironment { get; set; } = "prod";

    /// <summary>Webhook base URL (e.g., https://mydesk.digitalresponse.com.au or https://dev.digitalresponse.com.au)</summary>
    public string? WebhookBaseUrl { get; set; }

    /// <summary>Production webhook URL (auto-generated if not set)</summary>
    public string? ProdWebhookUrl { get; set; }

    /// <summary>Development webhook URL (auto-generated if not set)</summary>
    public string? DevWebhookUrl { get; set; }

    /// <summary>Custom commands mapping (trigger -> description)</summary>
    public Dictionary<string, string> CustomCommands { get; set; } = new();

    /// <summary>Enable voice message transcription</summary>
    public bool EnableVoiceTranscription { get; set; } = true;

    /// <summary>Enable markdown formatting in responses</summary>
    public bool EnableMarkdown { get; set; } = true;
}

public class NotificationSettings
{
    // SMS
    public bool EnableSms { get; set; } = false;
    public string SmsPrimaryProvider { get; set; } = "Twilio"; // Twilio
    public string? TwilioAccountSid { get; set; }
    public string? TwilioAuthToken { get; set; }
    public string? TwilioFromNumber { get; set; }
    public bool SmsFallbackToEmail { get; set; } = true; // fall back to email if SMS fails

    // Email
    public bool EnableEmail { get; set; } = true;
    public string EmailPrimaryProvider { get; set; } = "SendGrid"; // SendGrid, SMTP
    public string? SendGridApiKey { get; set; }
    public string? SendGridFromEmail { get; set; }
    public string? SendGridFromName { get; set; }

    // SMTP fallback
    public bool SmtpFallbackEnabled { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName { get; set; }

    // Trigger flags
    public bool NotifyOnInvoiceCreated { get; set; } = false;
    public bool NotifyOnInvoiceOverdue { get; set; } = false;
    public bool NotifyOnQuoteSent { get; set; } = false;
    public bool NotifyOnJobStatusChange { get; set; } = false;
    public bool NotifyOnDespatch { get; set; } = false;
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