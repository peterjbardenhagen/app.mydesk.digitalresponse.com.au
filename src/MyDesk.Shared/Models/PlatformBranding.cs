// PlatformBranding.cs - Multi-tenant branding resolver
// Determines which platform context to use based on hostname/subdomain

namespace MyDesk.Shared.Models;

public static class PlatformBranding
{
    public static string Title { get; set; } = "Techlight";
    
    public static ClientPlatform GetCurrentPlatform()
    {
        // This would typically resolve from hostname or user session
        // http://pb-legion/techlight -> Techlight
        // http://pb-legion/digital-response -> Digital Response
        // http://pb-legion/carter-capner-law -> Carter Capner Law
        
        return new ClientPlatform
        {
            Name = "Techlight",
            Slug = "techlight",
            BrandingName = "Techlight",
            PrimaryColor = "#00c8c8",
            EnableQuickBooksIntegration = false,
            EnableFrolloIntegration = false,
            EnableMYOBIntegration = true
        };
    }

    public static void SetPlatform(string slug)
    {
        slug = slug.ToLowerInvariant();
        Title = slug switch
        {
            "digital-response" or "digitalresponse" => "Digital Response",
            "carter-capner-law" or "cartercapnerlaw" => "Carter Capner Law",
            _ => "Techlight"
        };
    }
}