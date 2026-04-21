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
    
    // Copyright & Legal
    public string CopyrightText { get; set; } = "Copyright 2026 Digital Response. All rights reserved.";
    public string PrivacyPolicyUrl { get; set; } = "/privacy-policy";
    public string TermsAndConditionsUrl { get; set; } = "/terms-and-conditions";
    public string CookiePolicyUrl { get; set; } = "/cookie-policy";
    
    // Feature Flags
    public bool EnableAIAssistant { get; set; } = true;
    public bool EnableAskAI { get; set; } = true;
    public bool EnableTelegramBot { get; set; } = false;
    public bool EnableMYOBIntegration { get; set; } = true;
    public bool EnablePDFGeneration { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = true;
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
