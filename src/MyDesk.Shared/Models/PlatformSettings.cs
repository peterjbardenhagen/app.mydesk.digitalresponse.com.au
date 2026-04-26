namespace MyDesk.Shared.Models;

/// <summary>
/// Platform-wide settings for the MyDesk SaaS platform
/// This is the master configuration that each tenant can override
/// </summary>
public class PlatformSettings
{
    // Branding
    public string PlatformName { get; set; } = "MyDesk";
    public string PlatformTagline { get; set; } = "Business Management Platform";
    public string CompanyName { get; set; } = "Digital Response";
    public string CompanyLegalName { get; set; } = "Digital Response Pty Ltd";
    public string CompanyWebsite { get; set; } = "https://www.digitalresponse.com.au";
    public string SupportEmail { get; set; } = "support@digitalresponse.com.au";
    public string SalesEmail { get; set; } = "sales@digitalresponse.com.au";
    public string LogoUrl { get; set; } = "/images/mydesk-logo.svg";
    public string FaviconUrl { get; set; } = "/images/mydesk-favicon.svg";
    
    // Login Page Customization
    public string LoginLogoUrl { get; set; } = "/images/techlight-logo.svg";
    public string LoginMarkUrl { get; set; } = "/images/techlight-mark.svg";
    public string LoginQuote { get; set; } = "The intelligence layer for your entire lighting & electrical operation.";
    public string LoginQuoteAuthor { get; set; } = "Techlight MyDesk";
    public string LoginQuoteLabel { get; set; } = "Enterprise Portal";
    public string LoginHeading { get; set; } = "Welcome back";
    public string LoginSubheading { get; set; } = "Please enter your details to sign in.";
    public string LoginPrimaryColor { get; set; } = "#00C8C8"; // Tealy cyan
    public string LoginAccentColor { get; set; } = "#F59E0B"; // Amber/Orange
    public string LoginBackgroundColor { get; set; } = "#08121a"; // Dark background
    
    // Copyright & Legal
    public string CopyrightText { get; set; } = "Copyright 2026 Digital Response. All rights reserved.";
    public string PrivacyPolicyUrl { get; set; } = "/privacy-policy";
    public string TermsAndConditionsUrl { get; set; } = "/terms-and-conditions";
    public string CookiePolicyUrl { get; set; } = "/cookie-policy";
    
    // Feature Flags
    public bool EnableAIAssistant { get; set; } = true;
    public bool EnableAskAI { get; set; } = true;
    public bool EnableTelegramBot { get; set; } = false;
    public bool EnableXeroIntegration { get; set; } = false;
    public bool EnableQuickBooksIntegration { get; set; } = false;
    public bool EnableMYOBIntegration { get; set; } = true;
    public bool EnableOutlookIntegration { get; set; } = false;
    public bool EnableGoogleIntegration { get; set; } = false;
    public bool EnablePDFGeneration { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = true;
    public bool DisableAllEmails { get; set; } = false; // Kill switch for email sending (human-accessible)
    public bool EnableTwoFactorAuth { get; set; } = false;
    public bool EnableSSO { get; set; } = false;
    public bool EnableCustomBranding { get; set; } = false; // Premium feature
    
    // SaaS Configuration
    public bool IsMultiTenant { get; set; } = true;
    public int MaxUsersPerTenant { get; set; } = 50;
    public int MaxStoragePerTenantMB { get; set; } = 10240; // 10GB
    public int TrialDays { get; set; } = 14;
    public string SubscriptionPlan { get; set; } = "Professional";
    
    // Integration Settings
    public string DefaultTimezone { get; set; } = "Australia/Sydney";
    public string DefaultCurrency { get; set; } = "AUD";
    public string DefaultDateFormat { get; set; } = "dd/MM/yyyy";
    public string DefaultCountry { get; set; } = "AU";
    
    // Regional Settings
    public bool UseEnglishAustralianSpelling { get; set; } = true; // Use "analyse", "colour", etc.
    
    // SMTP Configuration (per tenant override possible)
    public SmtpSettings Smtp { get; set; } = new();
    
    // API Keys (encrypted at rest)
    public string? OpenAIApiKey { get; set; }
    public string? SendGridApiKey { get; set; }
    public string? TelegramBotToken { get; set; }
    
    // Version Info
    public string Version { get; set; } = "3.0.0";
    public string ReleaseDate { get; set; } = "2026-04-21";
    public string Environment { get; set; } = "Production";
    
    // Brand Assets - Dynamic file uploads for brand materials
    public List<BrandAssetFile> BrandAssets { get; set; } = new();
    
    // Important Links - Configurable links for marketing/resources
    public List<ImportantLink> ImportantLinks { get; set; } = new();
}

/// <summary>
/// Represents an uploaded brand asset file
/// </summary>
public class BrandAssetFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string Category { get; set; } = "General"; // Logo, Guidelines, Profile, Stationery, etc.
    public string Description { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = "";
    public bool IsPublic { get; set; } = true;
    
    public string FormattedSize => FileSize switch
    {
        >= 1_048_576 => $"{FileSize / 1_048_576.0:N1} MB",
        >= 1024 => $"{FileSize / 1024.0:N0} KB",
        _ => $"{FileSize} B"
    };
}

/// <summary>
/// Represents an important link for the Important Files & Links section
/// </summary>
public class ImportantLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public string Icon { get; set; } = "Link"; // MudBlazor icon name
    public string Category { get; set; } = "General";
    public int DisplayOrder { get; set; } = 0;
    public bool IsExternal { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.sendgrid.net";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "apikey";
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "noreply@digitalresponse.com.au";
    public string FromName { get; set; } = "MyDesk Platform";
}

/// <summary>
/// Tenant-specific settings that override platform defaults
/// </summary>
public class TenantSettings
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string TenantSubdomain { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? SubscriptionExpiryDate { get; set; }
    
    // Custom Branding (if enabled)
    public string? CustomLogoUrl { get; set; }
    public string? CustomPrimaryColor { get; set; }
    public string? CustomCompanyName { get; set; }
    
    // Feature Overrides
    public bool? EnableAIAssistant { get; set; }
    public bool? EnableAskAI { get; set; }
    public bool? EnableMYOBIntegration { get; set; }
    
    // Custom Domain (enterprise feature)
    public string? CustomDomain { get; set; }
}
